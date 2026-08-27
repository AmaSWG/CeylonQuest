using IdentityService.Data;
using IdentityService.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services;

/// <summary>
/// Orchestrates password reset flows:
/// 1. Forgot Password: Generate and store a reset token
/// 2. Reset Password: Validate token and update password
/// 
/// Note: Email sending is not yet implemented. For development/testing,
/// tokens can be retrieved via a debug endpoint (non-Production only).
/// </summary>
public class PasswordResetService
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordResetTokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        ApplicationDbContext db,
        PasswordResetTokenService tokenService,
        IEmailService emailService,
        IConfiguration config,
        ILogger<PasswordResetService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Initiates password reset flow for the given email.
    /// Generates reset token, stores hash with expiry, creates reset link, and sends reset email via IEmailService.
    /// Security: Does not expose whether the email exists (returns success with null token for non-existent emails).
    /// </summary>
    public async Task<(bool success, string? token)> InitiateForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, null);
        }

        var emailLower = email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower, cancellationToken);

        // Do NOT reveal whether email exists. Return success but do not send email.
        if (user == null)
        {
            _logger.LogInformation("Forgot password request for non-existent email: {Email}", emailLower);
            return (true, null);
        }

        try
        {
            var (plaintextToken, resetToken) = await _tokenService.CreateTokenAsync(user.Id);
            
            _logger.LogInformation(
                "Password reset token created for user {UserId} ({Email})",
                user.Id, user.Email);

            // Construct reset link using configured frontend base URL
            var baseUrl = _config["Email:ResetPasswordBaseUrl"] ?? _config["AppSettings:ResetPasswordBaseUrl"] ?? "http://localhost:5173/reset-password";
            var separator = baseUrl.Contains('?') ? "&" : "?";
            var resetLink = $"{baseUrl}{separator}token={Uri.EscapeDataString(plaintextToken)}";

            // Send password reset email via IEmailService
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink, cancellationToken);

            return (true, plaintextToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating password reset for user {UserId}", user.Id);
            return (false, null);
        }
    }

    /// <summary>
    /// Validates token and resets the user's password.
    /// </summary>
    public async Task<(bool success, string? errorMessage)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        // Validate request structure
        if (!request.IsValid(out var validationError))
        {
            return (false, validationError);
        }

        // Validate password strength (should already be validated by ModelState, but double-check)
        if (!PasswordValidator.IsValid(request.NewPassword))
        {
            return (false, PasswordValidator.GetRequirementsMessage());
        }

        // Validate token
        var resetToken = await _tokenService.ValidateTokenAsync(request.Token);
        if (resetToken == null)
        {
            _logger.LogWarning("Invalid or expired reset token attempted");
            return (false, "Password reset link is invalid or has expired. Please request a new password reset.");
        }

        if (resetToken.User == null)
        {
            _logger.LogError("Reset token {TokenId} has no associated user", resetToken.Id);
            return (false, "Password reset link is invalid or has expired. Please request a new password reset.");
        }

        try
        {
            var user = resetToken.User;

            // Hash the new password using the same hasher as elsewhere in the system
            var hasher = new PasswordHasher<IdentityService.Models.User>();
            user.PasswordHash = hasher.HashPassword(user, request.NewPassword);

            // Mark token as used (prevent reuse)
            await _tokenService.MarkAsUsedAsync(resetToken);

            // Save password change
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Password reset successful for user {UserId} ({Email})",
                user.Id, user.Email);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for token {TokenId}", resetToken.Id);
            return (false, "An error occurred while resetting your password. Please try again.");
        }
    }

    /// <summary>
    /// [DEVELOPMENT ONLY] Returns the plaintext token for testing.
    /// The API controller should use this to allow manual token retrieval in development.
    /// In production, tokens are sent via email only.
    /// </summary>
    public string? GetDevTokenForUser(Guid userId)
    {
        if (!IsDebugEnabled())
            return null;

        _logger.LogInformation("Debug token retrieval requested for user {UserId}", userId);
        return null; // Token is ephemeral and not stored; use InitiateForgotPasswordAsync instead
    }

    private bool IsDebugEnabled()
    {
        var environment = _config["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        return environment != "Production";
    }

    /// <summary>
    /// Helper for development: generates a fake token for testing without actual token generation.
    /// In production, this would be replaced with actual email sending.
    /// </summary>
    private string? GenerateDebugToken(Guid userId)
    {
        if (!IsDebugEnabled())
            return null;

        // In development, log a special marker so testing can retrieve via debug endpoint
        _logger.LogInformation("DEVELOPMENT: Password reset initiated for userId {UserId}. Use debug endpoint to retrieve token.", userId);
        return null; // Caller should use debug endpoint
    }
}

/// <summary>
/// Extension methods for ResetPasswordRequest validation.
/// </summary>
public static class ResetPasswordRequestExtensions
{
    public static bool IsValid(this ResetPasswordRequest request, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            errorMessage = "Password reset token is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            errorMessage = "New password is required.";
            return false;
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            errorMessage = "Passwords do not match.";
            return false;
        }

        return true;
    }
}
