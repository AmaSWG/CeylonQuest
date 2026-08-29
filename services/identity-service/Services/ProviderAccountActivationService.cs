using IdentityService.Data;
using IdentityService.Events;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Services;

public class ProviderAccountActivationService
{
    private readonly ApplicationDbContext _db;
    private readonly OtpService _otpService;
    private readonly ILogger<ProviderAccountActivationService> _logger;

    public ProviderAccountActivationService(
        ApplicationDbContext db,
        OtpService otpService,
        ILogger<ProviderAccountActivationService> logger)
    {
        _db = db;
        _otpService = otpService;
        _logger = logger;
    }

    public async Task ActivateApprovedProviderAsync(ProviderApproved approved, CancellationToken cancellationToken = default)
    {
        var emailLower = approved.Email.Trim().ToLower();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = approved.Email.Trim(),
                FirstName = approved.BusinessName.Trim(),
                Role = UserRole.Provider
            };
            _db.Users.Add(user);
            _logger.LogInformation(
                "Creating new Identity account for approved provider {Email} (application {ApplicationId})",
                user.Email, approved.ApplicationId);
        }
        else
        {
            user.Role = UserRole.Provider;
            _logger.LogInformation(
                "Promoting existing Identity account {Email} to Provider (application {ApplicationId})",
                user.Email, approved.ApplicationId);
        }

        user.OtpCode = _otpService.GenerateCode();
        user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpService.DefaultExpiryMinutes);
        user.RequiresPasswordChange = true;
        user.IsActive = false;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
