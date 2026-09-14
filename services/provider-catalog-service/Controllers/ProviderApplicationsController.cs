using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;
using ProviderCatalogService.Services;
using Shared.Storage;


[ApiController]
[Route("api/catalog/provider-applications")]
public class ProviderApplicationsController : ControllerBase
{
    private readonly CatalogDbContext _db;
    private readonly IBlobStorageService _blobStorage;
    private readonly string _containerName;
    public ProviderApplicationsController(
        CatalogDbContext db,
        IBlobStorageService blobStorage,
        IConfiguration configuration)
    {
        _db = db;
        _blobStorage = blobStorage;
        _containerName = configuration["AzureStorage:VerificationFilesContainer"] ?? "provider-verification-files";
    }
    [HttpPost]
    public async Task<IActionResult> Submit([FromForm] CreateProviderApplicationRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        // Gather all uploaded files
        var files = new List<IFormFile>();
        if (request.LegalDocuments != null && request.LegalDocuments.Count > 0)
            files.AddRange(request.LegalDocuments.Where(f => f.Length > 0));
        if (request.LegalDocument != null && request.LegalDocument.Length > 0 && !files.Contains(request.LegalDocument))
            files.Add(request.LegalDocument);
        // Validation: 1 is mandatory, max 5 allowed
        if (files.Count == 0)
            return BadRequest(new { message = "At least one legal verification document is required." });
        if (files.Count > 5)
            return BadRequest(new { message = "You can upload a maximum of 5 verification documents." });
        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var uploadedDocMetadata = new List<object>();
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return BadRequest(new { message = $"File '{file.FileName}' has an invalid format. Only PDF, JPG, and PNG are accepted." });
            if (file.Length > 10 * 1024 * 1024) // 10MB limit
                return BadRequest(new { message = $"File '{file.FileName}' exceeds the 10MB file size limit." });
            var blobName = $"{Guid.NewGuid()}{ext}";
            var contentType = ext == ".pdf" ? "application/pdf" : (ext == ".png" ? "image/png" : "image/jpeg");
            await using var stream = file.OpenReadStream();
            await _blobStorage.UploadAsync(stream, blobName, _containerName, contentType);
            uploadedDocMetadata.Add(new
            {
                BlobName = blobName,
                OriginalFileName = file.FileName
            });
        }
        var application = new ProviderApplication
        {
            Id = Guid.NewGuid(),
            BusinessName = request.BusinessName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber.Trim(),
            ServiceType = request.ServiceType.Trim(),
            Location = request.Location.Trim(),
            Description = request.Description.Trim(),
            Status = ProviderStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
            LegalDocumentsJson = JsonSerializer.Serialize(uploadedDocMetadata),
            LegalDocumentFileName = files[0].FileName
        };
        _db.ProviderApplications.Add(application);
        await _db.SaveChangesAsync();
        return Created($"/api/catalog/provider-applications/status?email={application.Email}", new
        {
            applicationId = application.Id,
            status = application.Status,
            documentsUploaded = files.Count,
            submittedAt = application.SubmittedAt
        });
    }
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required." });
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var application = await _db.ProviderApplications.FirstOrDefaultAsync(a => a.Email == normalizedEmail);
        if (application is null)
            return NotFound(new { message = "No provider application found." });
        return Ok(new
        {
            applicationId = application.Id,
            businessName = application.BusinessName,
            status = application.Status,
            rejectionReason = application.RejectionReason,
            submittedAt = application.SubmittedAt,
            reviewedAt = application.ReviewedAt
        });
    }
}
