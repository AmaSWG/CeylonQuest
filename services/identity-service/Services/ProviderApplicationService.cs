using IdentityService.Data;
using Microsoft.EntityFrameworkCore;
using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Services;

public class DuplicateApplicationException : Exception
{
    public DuplicateApplicationException(string? message = null) : base(message) { }
}

public class ProviderApplicationService
{
    private readonly ApplicationDbContext _db;

    public ProviderApplicationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ProviderApplication> CreateAsync(ProviderApplicationRequest request)
    {
        var emailLower = (request.Email ?? string.Empty).Trim().ToLower();

        var exists = await _db.ProviderApplications.AnyAsync(p => p.Email.ToLower() == emailLower);
        if (exists)
        {
            throw new DuplicateApplicationException("An application with this email already exists.");
        }

        var app = new ProviderApplication
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName?.Trim() ?? string.Empty,
            LastName = request.LastName?.Trim() ?? string.Empty,
            Email = request.Email?.Trim() ?? string.Empty,
            PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty,
            BusinessName = request.BusinessName?.Trim() ?? string.Empty,
            ServiceType = request.ServiceType?.Trim() ?? string.Empty,
            Location = request.Location?.Trim() ?? string.Empty,
            Description = request.Description?.Trim() ?? string.Empty,
            LegalDocumentFileName = request.LegalDocumentFileName?.Trim(),
            Status = ProviderApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.ProviderApplications.Add(app);
        await _db.SaveChangesAsync();

        return app;
    }

    public async Task<List<ProviderApplication>> GetAllAsync(int limit = 50)
    {
        return await _db.ProviderApplications
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<ProviderApplicationStatusResponse?> GetStatusByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var emailLower = email.Trim().ToLower();

        var app = await _db.ProviderApplications
            .Where(p => p.Email.ToLower() == emailLower)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        // Also check if an active or approved provider account already exists in Users table
        var providerUser = await _db.Users
            .Where(u => u.Email.ToLower() == emailLower && u.Role == UserRole.Provider)
            .FirstOrDefaultAsync();

        if (app == null && providerUser == null)
        {
            return null;
        }

        // If user is already an active/registered provider or application status is Approved (1)
        var isApproved = (app != null && app.Status == ProviderApplicationStatus.Approved) || providerUser != null;
        var isRejected = app != null && app.Status == ProviderApplicationStatus.Rejected && providerUser == null;
        var isPending = app != null && app.Status == ProviderApplicationStatus.Pending && providerUser == null;

        var statusString = isApproved ? "Approved" : (isRejected ? "Rejected" : "Pending");

        var message = statusString switch
        {
            "Approved" => "Congratulations! Your provider application has been approved. Please check your email for your account activation code or log in to your provider portal.",
            "Rejected" => "Your provider application was not approved.",
            _ => "Your application has been received and is currently under review by the CeylonQuest team."
        };

        var businessName = app?.BusinessName;
        if (string.IsNullOrWhiteSpace(businessName) && providerUser != null)
        {
            businessName = $"{providerUser.FirstName} {providerUser.LastName}".Trim();
        }

        return new ProviderApplicationStatusResponse
        {
            Email = app?.Email ?? providerUser!.Email,
            BusinessName = string.IsNullOrWhiteSpace(businessName) ? "Provider Account" : businessName,
            ServiceType = app?.ServiceType ?? "Tourism Service Provider",
            Status = statusString,
            RejectionReason = isRejected ? app?.RejectionReason : null,
            SubmittedAt = app?.CreatedAt ?? providerUser!.CreatedAt,
            Message = message
        };
    }
}
