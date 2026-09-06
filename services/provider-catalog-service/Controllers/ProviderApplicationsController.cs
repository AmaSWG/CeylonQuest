using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;
using ProviderCatalogService.Services;

namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/provider-applications")]
public class ProviderApplicationsController : ControllerBase
{
    private readonly CatalogDbContext _db;
    private readonly DocumentStorageService _documentStorage;

    public ProviderApplicationsController(
        CatalogDbContext db,
        DocumentStorageService documentStorage)
    {
        _db = db;
        _documentStorage = documentStorage;
    }

    [HttpPost]
    public async Task<IActionResult> Submit(
        [FromForm] CreateProviderApplicationRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var application = new ProviderApplication
        {
            Id = Guid.NewGuid(),
            BusinessName = request.BusinessName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            ServiceType = request.ServiceType.Trim(),
            Location = request.Location.Trim(),
            Description = request.Description.Trim(),
            Status = ProviderStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };

        if (request.LegalDocument is not null)
        {
            var stored = await _documentStorage.SaveAsync(
                request.LegalDocument
            );

            application.LegalDocumentPath = stored.StoredPath;
            application.LegalDocumentFileName = stored.OriginalFileName;
        }

        _db.ProviderApplications.Add(application);

        await _db.SaveChangesAsync();

        return Created(
            $"/api/catalog/provider-applications/status?email={application.Email}",
            new
            {
                applicationId = application.Id,
                status = application.Status,
                submittedAt = application.SubmittedAt
            }
        );
    }
	
	[HttpGet("status")]
	public async Task<IActionResult> GetStatus(
		[FromQuery] string email)
	{
		if (string.IsNullOrWhiteSpace(email))
			return BadRequest(new { message = "Email is required." });

		var normalizedEmail = email.Trim().ToLowerInvariant();
		
		var application = await _db.ProviderApplications
			.FirstOrDefaultAsync(a =>
				a.Email == normalizedEmail);

		if (application is null)
			return NotFound(new
			{
				message = "No provider application found."
			});

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