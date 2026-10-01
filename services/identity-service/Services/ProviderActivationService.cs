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
    private readonly OtpService _otpService;
    private readonly IEmailService _emailService;

    public ProviderActivationService(
        ApplicationDbContext db,
        OtpService otpService,
        IEmailService emailService)
    {
        _db = db;
        _passwordHasher = new PasswordHasher<User>();
        _otpService = otpService;
        _emailService = emailService;
    }

    public async Task ActivateAsync(ProviderActivateRequest request)
    {
		
		var emailLower = request.Email.Trim().ToLower();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);

        if (user is null || string.IsNullOrEmpty(user.OtpCode))
            throw new InvalidOtpException("Invalid or missing OTP.");
		
		if (user.OtpExpiresAt.HasValue && DateTime.SpecifyKind(user.OtpExpiresAt.Value, DateTimeKind.Utc) < DateTime.UtcNow)
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
	
	public async Task VerifyOtpAsync(string email, string otp)
	{
		var emailLower = email.Trim().ToLower();
		
		var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);
		
		if (user is null || string.IsNullOrEmpty(user.OtpCode))
			throw new InvalidOtpException("Invalid or missing OTP.");
		
		if (user.OtpExpiresAt.HasValue && DateTime.SpecifyKind(user.OtpExpiresAt.Value, DateTimeKind.Utc) < DateTime.UtcNow)
			throw new ExpiredOtpException("OTP has expired. Please click Resend OTP to get a new code.");
		
		if (!string.Equals(user.OtpCode, otp.Trim(), StringComparison.Ordinal))
			throw new InvalidOtpException("Invalid OTP code. Please recheck your email.");
	}

    public async Task ResendOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailLower = email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower, cancellationToken);

        if (user is null || user.Role != UserRole.Provider)
        {
            // For security, do not disclose non-existent accounts
            return;
        }

        if (user.IsActive && !user.RequiresPasswordChange)
        {
            throw new InvalidOperationException("Account is already activated. Please proceed to login.");
        }

        user.OtpCode = _otpService.GenerateCode();
        user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpService.DefaultExpiryMinutes);
        user.RequiresPasswordChange = true;
        user.IsActive = false;

        await _db.SaveChangesAsync(cancellationToken);

        await _emailService.SendProviderOtpEmailAsync(
            user.Email,
            user.FirstName ?? "Provider",
            user.OtpCode,
            cancellationToken
        );
    }
	
}