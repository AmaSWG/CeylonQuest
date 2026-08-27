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
}
