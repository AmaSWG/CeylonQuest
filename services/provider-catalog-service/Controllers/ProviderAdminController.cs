using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;
using ProviderCatalogService.Services;
using Shared.Kafka;
using Shared.Storage;

namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/admin/providers")]
[Authorize(Roles = "Admin")]
public class ProviderAdminController : ControllerBase
{
    private readonly CatalogDbContext _db;
    private readonly IKafkaProducer _kafkaProducer;
	private readonly IEmailService _emailService;
	private readonly IBlobStorageService _blobStorage;
    private readonly string _verificationContainer;

    public ProviderAdminController(
        CatalogDbContext db,
        IKafkaProducer kafkaProducer,
        IEmailService emailService,
        IBlobStorageService blobStorage,
        IConfiguration configuration)
    {
        _db = db;
        _kafkaProducer = kafkaProducer;
		_emailService = emailService;
		_blobStorage = blobStorage;
        _verificationContainer = configuration["AzureStorage:VerificationFilesContainer"] ?? "provider-verification-files";
    }

    /// <summary>
    /// Retrieves provider applications, optionally filtered by their review status
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetApplications(
        [FromQuery] ProviderStatus? status)
    {
        var query = _db.ProviderApplications
            .AsNoTracking()
            .AsQueryable();

        // Apply the optional status filter before projecting the results
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
                a.PhoneNumber,
                a.ServiceType,
                a.Location,
                a.Description,
                a.LegalDocumentFileName,
                a.LegalDocumentsJson,
                a.Status,
                a.SubmittedAt,
                a.ReviewedAt,
                a.RejectionReason
            })
            .ToListAsync();

        return Ok(applications);
    }

    /// <summary>
    /// Streams a specific verification document for an application by index,
    /// resolving the blob from Azure Storage metadata or the legacy path
    /// </summary>
    [HttpGet("{id:guid}/document")]
    public async Task<IActionResult> DownloadDocument(Guid id, [FromQuery] int index = 0)
    {
        var application = await _db.ProviderApplications
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null)
            return NotFound(new { message = "Application not found." });

        string? blobName = null;
        string fileName = application.LegalDocumentFileName ?? "Verification_Document.pdf";

        // Try reading from the new Azure Blob JSON metadata
        if (!string.IsNullOrWhiteSpace(application.LegalDocumentsJson) && application.LegalDocumentsJson != "[]")
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<JsonElement>>(application.LegalDocumentsJson);
                if (parsed != null && parsed.Count > 0)
                {
                    var docIndex = Math.Clamp(index, 0, parsed.Count - 1);
                    var target = parsed[docIndex];
                    blobName = target.GetProperty("BlobName").GetString();
                    if (target.TryGetProperty("OriginalFileName", out var origName))
                    {
                        fileName = origName.GetString() ?? fileName;
                    }
                }
            }
            catch { }
        }

        // Fallback to legacy path if present
        if (string.IsNullOrWhiteSpace(blobName) && !string.IsNullOrWhiteSpace(application.LegalDocumentPath))
        {
            blobName = Path.GetFileName(application.LegalDocumentPath);
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            return NotFound(new { message = "No verification document was found for this application." });
        }

        // Open stream directly from Azure Blob Storage
        var stream = await _blobStorage.OpenReadAsync(blobName, _verificationContainer);
        if (stream == null)
        {
            return NotFound(new { message = "Document file was not found in Azure Storage." });
        }

        // Resolve the content type based on the file extension for proper browser handling
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        // Return file stream with proper download filename
        return File(stream, contentType, fileName);
    }

    /// <summary>
    /// Approves a pending provider application, creates the provider profile,
    /// and publishes a provider approved event
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var application = await _db.ProviderApplications
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null)
            return NotFound();

        // Only applications still awaiting review can be approved
        if (application.Status != ProviderStatus.Pending)
        {
            return BadRequest(new
            {
                message = "Only pending applications can be approved."
            });
        }

        var existingProvider = await _db.Providers
            .FirstOrDefaultAsync(p => p.Email == application.Email);

        // Prevent duplicate provider profiles for the same email address
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
            PhoneNumber = application.PhoneNumber,
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

        // Notify downstream services that the provider has been approved
        await _kafkaProducer.PublishAsync("provider.approved", application.Id.ToString(), payload);

        return Ok(new
        {
            ApplicationId = application.Id,
            Status = application.Status,
            ProviderId = provider.Id,
            ReviewedAt = application.ReviewedAt
        });
    }

    /// <summary>
    /// Rejects a pending provider application with a required reason
    /// and notifies the applicant by email
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectProviderApplicationRequest request)
    {
        var application = await _db.ProviderApplications
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null)
            return NotFound();

        // Only applications still awaiting review can be rejected
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
		application.ReviewedAt = DateTime.UtcNow;
        application.RejectionReason = request.RejectionReason.Trim();

        await _db.SaveChangesAsync();
		
		await _emailService.SendApplicationRejectionEmailAsync(
			application.Email,
			application.BusinessName,
			application.RejectionReason
		);

        return Ok(new
        {
            application.Id,
            application.Status,
            application.RejectionReason,
            application.ReviewedAt
        });
    }

    /// <summary>
    /// Generates short-lived SAS links for all verification documents
    /// attached to an application so the admin can preview them securely
    /// </summary>
    [HttpGet("{id:guid}/documents")]
    public async Task<IActionResult> GetApplicationDocuments(Guid id)
    {
        var app = await _db.ProviderApplications.FirstOrDefaultAsync(a => a.Id == id);
        if (app == null) return NotFound(new { message = "Application not found." });
        var docs = new List<object>();
        if (!string.IsNullOrWhiteSpace(app.LegalDocumentsJson) && app.LegalDocumentsJson != "[]")
        {
            var parsed = JsonSerializer.Deserialize<List<JsonElement>>(app.LegalDocumentsJson) ?? new();
            foreach (var doc in parsed)
            {
                var blobName = doc.GetProperty("BlobName").GetString() ?? "";
                var originalName = doc.GetProperty("OriginalFileName").GetString() ?? blobName;

                // Generate a temporary 30 minute SAS link for the Admin
                var secureSasUrl = _blobStorage.GenerateSasUri(blobName, _verificationContainer, TimeSpan.FromMinutes(30));
                docs.Add(new { fileName = originalName, url = secureSasUrl });
            }
        }
        else if (!string.IsNullOrWhiteSpace(app.LegalDocumentPath))
        {
            var blobName = Path.GetFileName(app.LegalDocumentPath);
            var secureSasUrl = _blobStorage.GenerateSasUri(blobName, _verificationContainer, TimeSpan.FromMinutes(30));
            docs.Add(new { fileName = app.LegalDocumentFileName ?? "Document.pdf", url = secureSasUrl });
        }
        return Ok(docs);
    }
}