using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;

namespace ProviderCatalogService.Controllers;

public record UpdateProviderProfileRequest(
    string BusinessName,
    string ServiceType,
    string Location,
    string Description,
    string? PhoneNumber
);

[ApiController]
[Route("api/catalog/provider/profile")]
[Authorize(Roles = "Provider")]
public class ProviderProfileController : ControllerBase
{
    private readonly CatalogDbContext _db;

    public ProviderProfileController(CatalogDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProfile()
    {
        var identityUserId = GetIdentityUserId();
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

        var provider = await _db.Providers.FirstOrDefaultAsync(p =>
            (identityUserId.HasValue && p.IdentityUserId == identityUserId.Value) ||
            (!string.IsNullOrEmpty(email) && p.Email.ToLower() == email.ToLower()));

        if (provider is null)
            return NotFound(new { message = "Provider business profile not found." });

        // Auto-link IdentityUserId if missing
        if (provider.IdentityUserId == null && identityUserId.HasValue)
        {
            provider.IdentityUserId = identityUserId.Value;
            await _db.SaveChangesAsync();
        }

        return Ok(new
        {
            provider.Id,
            provider.BusinessName,
            provider.Email,
            provider.PhoneNumber,
            provider.ServiceType,
            provider.Location,
            provider.Description,
            provider.LegalDocumentFileName,
            provider.CreatedAt
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProviderProfileRequest request)
    {
        var identityUserId = GetIdentityUserId();
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

        var provider = await _db.Providers.FirstOrDefaultAsync(p =>
            (identityUserId.HasValue && p.IdentityUserId == identityUserId.Value) ||
            (!string.IsNullOrEmpty(email) && p.Email.ToLower() == email.ToLower()));

        if (provider is null)
            return NotFound(new { message = "Provider business profile not found." });

        provider.BusinessName = request.BusinessName.Trim();
        provider.ServiceType = request.ServiceType.Trim();
        provider.Location = request.Location.Trim();
        provider.Description = request.Description.Trim();
        provider.PhoneNumber  = request.PhoneNumber?.Trim();

        await _db.SaveChangesAsync();

        return Ok(new
        {
            provider.Id,
            provider.BusinessName,
            provider.Email,
            provider.PhoneNumber,                                // <-- ADD
            provider.ServiceType,
            provider.Location,
            provider.Description,
            provider.LegalDocumentFileName,
            provider.CreatedAt
        });
    }

    private Guid? GetIdentityUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}