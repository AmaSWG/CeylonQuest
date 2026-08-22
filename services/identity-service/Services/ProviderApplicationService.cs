using IdentityService.Data;
using Microsoft.EntityFrameworkCore;
using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Services;

public class ProviderApplicationService
{
    private readonly ApplicationDbContext _db;

    public ProviderApplicationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ProviderApplication> CreateAsync(ProviderApplicationRequest request)
    {
        var app = new ProviderApplication
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            BusinessName = request.BusinessName,
            ServiceType = request.ServiceType,
            Location = request.Location,
            Description = request.Description,
            LegalDocumentFileName = request.LegalDocumentFileName,
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
