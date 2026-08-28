using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services;

public class InvalidOtpException : Exception
{
    public InvalidOtpException(string? message = null) : base(message) { }
}

public class ExpiredOtpException : Exception
{
    public ExpiredOtpException(string? message = null) : base(message) { }
}

/// <summary>
/// Handles the HTTP-triggered provider account activation flow:
/// verifies an OTP, sets the provider's chosen password, and activates the account.
/// This is separate from <see cref="ProviderAccountActivationService"/>, which handles
/// the Kafka-triggered side effects of an approved provider application.
/// </summary>
public class ProviderActivationService
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordHasher<User> _passwordHasher;

    public ProviderActivationService(ApplicationDbContext db)
    {
        _db = db;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task ActivateAsync(ProviderActivateRequest request)
    {
        var emailLower = request.Email.Trim().ToLower();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);

        // Treat missing user as invalid OTP to avoid email enumeration.
        if (user is null || string.IsNullOrEmpty(user.OtpCode))
            throw new InvalidOtpException("Invalid or missing OTP.");

        // Check expiry before comparing value to give a more specific error.
        if (user.OtpExpiresAt.HasValue && user.OtpExpiresAt.Value < DateTime.UtcNow)
            throw new ExpiredOtpException("OTP has expired.");

        // Constant-time comparison is not strictly required for 6-digit numeric OTPs,
        // but we use ordinal comparison to avoid any locale-based surprises.
        if (!string.Equals(user.OtpCode, request.Otp.Trim(), StringComparison.Ordinal))
            throw new InvalidOtpException("Invalid OTP.");

        // Set the new password using the same hasher used elsewhere in the service.
        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);

        // Update personal profile information if provided during activation
        if (!string.IsNullOrWhiteSpace(request.FirstName))
            user.FirstName = request.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(request.LastName))
            user.LastName = request.LastName.Trim();
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            user.PhoneNumber = request.PhoneNumber.Trim();

        // Activate the account and clear the one-time-password fields.
        user.RequiresPasswordChange = false;
        user.IsActive = true;
        user.OtpCode = null;
        user.OtpExpiresAt = null;

        await _db.SaveChangesAsync();
    }
}
