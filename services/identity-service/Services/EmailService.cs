using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdentityService.Services;

/// <summary>
/// Email service dedicated strictly to sending password-reset emails.
/// Does not handle OTPs, provider registration, or account activation.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly IHostEnvironment? _env;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration config,
        ILogger<EmailService> logger,
        IHostEnvironment? env = null)
    {
        _config = config;
        _logger = logger;
        _env = env;
    }

    public async Task SendPasswordResetEmailAsync(
        string recipientEmail,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new ArgumentException("Recipient email cannot be empty.", nameof(recipientEmail));
        }

        if (string.IsNullOrWhiteSpace(resetLink))
        {
            throw new ArgumentException("Reset link cannot be empty.", nameof(resetLink));
        }

        var isDevelopment = IsDevelopmentEnvironment();
        var host = _config["Email:Host"];
        var portStr = _config["Email:Port"];
        var port = int.TryParse(portStr, out var p) ? p : 587;
        var username = _config["Email:Username"];
        var password = _config["Email:Password"];
        var fromAddress = _config["Email:From"] ?? "noreply@ceylonquest.com";
        var fromName = _config["Email:FromName"] ?? "CeylonQuest";
        var enableSsl = bool.TryParse(_config["Email:EnableSsl"], out var ssl) ? ssl : true;

        var subject = "Reset your CeylonQuest password";
        var body = $@"Hello,

We received a request to reset your CeylonQuest password.

Click the link below to create a new password:

Reset Password

{resetLink}

This link will expire after the configured amount of time.

If you did not request a password reset, you can safely ignore this email.";

        // If SMTP host or credentials are not configured
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            if (isDevelopment)
            {
                // Development fallback: Log the link to console for local developer testing
                _logger.LogInformation("[DEV] Password reset email for {Email}", recipientEmail);
                _logger.LogInformation("[DEV] Reset link: {ResetLink}", resetLink);
                return;
            }

            _logger.LogError("SMTP credentials not configured. Cannot send password reset email in production.");
            return;
        }

        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(fromAddress, fromName);
            message.To.Add(new MailAddress(recipientEmail));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = false;

            using var client = new SmtpClient(host, port);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(username, password);

            // Register cancellation token
            using (cancellationToken.Register(() => client.SendAsyncCancel()))
            {
                await client.SendMailAsync(message);
            }

            _logger.LogInformation("Password reset email sent successfully to {Email}", recipientEmail);
        }
        catch (Exception ex)
        {
            if (isDevelopment)
            {
                // Development fallback: Log link so local testing continues without crash
                _logger.LogWarning(ex, "[DEV] SMTP send failed; falling back to console output.");
                _logger.LogInformation("[DEV] Password reset email for {Email}", recipientEmail);
                _logger.LogInformation("[DEV] Reset link: {ResetLink}", resetLink);
                return;
            }

            _logger.LogError(ex, "Failed to send password reset email to {Email}", recipientEmail);
            // In production, do not crash or leak tokens
        }
    }

    private bool IsDevelopmentEnvironment()
    {
        if (_env != null)
        {
            return _env.IsDevelopment() || _env.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase);
        }

        var envVar = _config["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.Equals(envVar, "Development", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(envVar, "Testing", StringComparison.OrdinalIgnoreCase);
    }
}
