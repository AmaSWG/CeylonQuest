using IdentityService.Data;
using IdentityService.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services;

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

    public async Task<(bool success, string? token)> InitiateForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, null);
        }

        var emailLower = email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower, cancellationToken);

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

            var baseUrl = _config["Email:ResetPasswordBaseUrl"] ?? _config["AppSettings:ResetPasswordBaseUrl"] ?? "http://localhost:5173/reset-password";
            var separator = baseUrl.Contains('?') ? "&" : "?";
            var resetLink = $"{baseUrl}{separator}token={Uri.EscapeDataString(plaintextToken)}";

            await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink, cancellationToken);

            return (true, plaintextToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating password reset for user {UserId}", user.Id);
            return (false, null);
        }
    }

    public async Task<(bool success, string? errorMessage)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (!request.IsValid(out var validationError))
        {
            return (false, validationError);
        }

        if (!PasswordValidator.IsValid(request.NewPassword))
        {
            return (false, PasswordValidator.GetRequirementsMessage());
        }

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

            var hasher = new PasswordHasher<IdentityService.Models.User>();
            user.PasswordHash = hasher.HashPassword(user, request.NewPassword);

            await _tokenService.MarkAsUsedAsync(resetToken);

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

    public string? GetDevTokenForUser(Guid userId)
    {
        if (!IsDebugEnabled())
            return null;

        _logger.LogInformation("Debug token retrieval requested for user {UserId}", userId);
        return null;
    }

    private bool IsDebugEnabled()
    {
        var environment = _config["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        return environment != "Production";
    }

    private string? GenerateDebugToken(Guid userId)
    {
        if (!IsDebugEnabled())
            return null;

        _logger.LogInformation("DEVELOPMENT: Password reset initiated for userId {UserId}. Use debug endpoint to retrieve token.", userId);
        return null;
    }
}

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
