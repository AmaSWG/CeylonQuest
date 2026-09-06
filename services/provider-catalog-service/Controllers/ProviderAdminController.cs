using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;
using Shared.Kafka;

namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/admin/providers")]
[Authorize(Roles = "Admin")]
public class ProviderAdminController : ControllerBase
{
    private readonly CatalogDbContext _db;
    private readonly IKafkaProducer _kafkaProducer;

    public ProviderAdminController(CatalogDbContext db, IKafkaProducer kafkaProducer)
    {
        _db = db;
        _kafkaProducer = kafkaProducer;
    }

    [HttpGet]
    public async Task<IActionResult> GetApplications(
        [FromQuery] ProviderStatus? status)
    {
        var query = _db.ProviderApplications
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var applications = await query
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new
            {
                a.Id,
                a.BusinessName,
                a.Email,
                a.ServiceType,
                a.Location,
                a.Description,
                a.Status,
                a.SubmittedAt,
                a.ReviewedAt,
                a.RejectionReason
            })
            .ToListAsync();

        return Ok(applications);
    }

    [HttpGet("{id:guid}/document")]
    public async Task<IActionResult> DownloadDocument(Guid id)
    {
        var application = await _db.ProviderApplications
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(application.LegalDocumentPath))
            return NotFound(new
            {
                message = "No document was uploaded."
            });

        var fullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            application.LegalDocumentPath
        );

        if (!System.IO.File.Exists(fullPath))
            return NotFound(new
            {
                message = "Document file was not found."
            });

        var contentType = "application/octet-stream";
        var extension = Path.GetExtension(fullPath);

        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            contentType = "application/pdf";

        return PhysicalFile(
            fullPath,
            contentType,
            application.LegalDocumentFileName
        );
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var application = await _db.ProviderApplications
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null)
            return NotFound();

        if (application.Status != ProviderStatus.Pending)
        {
            return BadRequest(new
            {
                message = "Only pending applications can be approved."
            });
        }

        var existingProvider = await _db.Providers
            .FirstOrDefaultAsync(p => p.Email == application.Email);

        if (existingProvider is not null)
        {
            return Conflict(new
            {
                message = "A provider with this email already exists."
            });
        }

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            BusinessName = application.BusinessName,
            Email = application.Email,
            ServiceType = application.ServiceType,
            Location = application.Location,
            Description = application.Description,
            LegalDocumentPath = application.LegalDocumentPath,
            LegalDocumentFileName = application.LegalDocumentFileName,
            IdentityUserId = null,
            CreatedAt = DateTime.UtcNow
        };

        _db.Providers.Add(provider);

        application.Status = ProviderStatus.Approved;
        application.ReviewedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var payload = new
        {
            ApplicationId = application.Id,
            Email = application.Email,
            BusinessName = application.BusinessName,
            ApprovedAt = application.ReviewedAt.Value
        };

        await _kafkaProducer.PublishAsync("provider.approved", application.Id.ToString(), payload);

        return Ok(new
        {
            ApplicationId = application.Id,
            Status = application.Status,
            ProviderId = provider.Id,
            ReviewedAt = application.ReviewedAt
        });
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectProviderApplicationRequest request)
    {
        var application = await _db.ProviderApplications
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null)
            return NotFound();

        if (application.Status != ProviderStatus.Pending)
        {
            return BadRequest(new
            {
                message = "Only pending applications can be rejected."
            });
        }

        if (string.IsNullOrWhiteSpace(request.RejectionReason))
        {
            return BadRequest(new
            {
                message = "A rejection reason is required."
            });
        }

        application.Status = ProviderStatus.Rejected;
        application.RejectionReason = request.RejectionReason.Trim();
        application.ReviewedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            application.Id,
            application.Status,
            application.RejectionReason,
            application.ReviewedAt
        });
    }
}