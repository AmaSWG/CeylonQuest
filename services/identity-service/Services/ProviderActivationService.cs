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

        if (user is null || string.IsNullOrEmpty(user.OtpCode))
            throw new InvalidOtpException("Invalid or missing OTP.");

        if (user.OtpExpiresAt.HasValue && user.OtpExpiresAt.Value < DateTime.UtcNow)
            throw new ExpiredOtpException("OTP has expired.");

        if (!string.Equals(user.OtpCode, request.Otp.Trim(), StringComparison.Ordinal))
            throw new InvalidOtpException("Invalid OTP.");

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);

        user.RequiresPasswordChange = false;
        user.IsActive = true;
        user.OtpCode = null;
        user.OtpExpiresAt = null;

        await _db.SaveChangesAsync();
    }
}
