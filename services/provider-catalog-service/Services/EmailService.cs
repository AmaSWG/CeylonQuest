using System.Net;
using System.Net.Mail;

namespace ProviderCatalogService.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly IHostEnvironment? _env;

    public EmailService(
        IConfiguration config, 
        ILogger<EmailService> logger, 
        IHostEnvironment? env = null)
    {
        _config = config;
        _logger = logger;
        _env = env;
    }

    public async Task SendApplicationRejectionEmailAsync(
        string recipientEmail,
        string businessName,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var isDevelopment = _env?.IsDevelopment() ?? true;
        var host = _config["Email:Host"];
        var portStr = _config["Email:Port"];
        var port = int.TryParse(portStr, out var p) ? p : 587;
        var username = _config["Email:Username"];
        var password = _config["Email:Password"];
        var fromAddress = _config["Email:From"] ?? "noreply@ceylonquest.com";
        var fromName = _config["Email:FromName"] ?? "CeylonQuest";
        var enableSsl = bool.TryParse(_config["Email:EnableSsl"], out var ssl) ? ssl : true;

        var subject = "Update Regarding Your CeylonQuest Provider Application";
        var body = $@"Hello {businessName},

A Ceylonquest admin has reviewed your application and legal documentation.

Unfortunately, we are unable to approve your application at this time.

Reason for rejection:

{rejectionReason}


If you have questions or would like to re-apply with updated information, please feel free to submit a new application or contact our support team.

Best regards,
The CeylonQuest Admin Team";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            if (isDevelopment)
            {
                _logger.LogInformation("[DEV] Application Rejection email for {Email} ({Business})", recipientEmail, businessName);
                _logger.LogInformation("[DEV] Reason: {Reason}", rejectionReason);
                return;
            }
            _logger.LogError("SMTP credentials not configured. Cannot send rejection email in production.");
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

            using (cancellationToken.Register(() => client.SendAsyncCancel()))
            {
                await client.SendMailAsync(message);
            }

            _logger.LogInformation("Application rejection email sent to {Email}", recipientEmail);
        }
        catch (Exception ex)
        {
            if (isDevelopment)
            {
                _logger.LogWarning(ex, "[DEV] SMTP send failed; falling back to console output.");
                _logger.LogInformation("[DEV] Rejection email for {Email}: {Reason}", recipientEmail, rejectionReason);
                return;
            }
            _logger.LogError(ex, "Failed to send application rejection email to {Email}", recipientEmail);
        }
    }
}