using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;

namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/activity-listings")]
[Authorize(Roles = "Provider")]
public class ActivityListingsController : ControllerBase
{
    private readonly CatalogDbContext _db;

    public ActivityListingsController(CatalogDbContext db)
    {
        _db = db;
    }

    
    // Create Listing 
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateActivityListingRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
        {
            return StatusCode(403, new { message = errorMessage });
        }

        var listing = new ActivityListing
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Unit = request.Unit.Trim(),
            Location = request.Location.Trim(),
            MaxParticipants = request.MaxParticipants > 0 ? request.MaxParticipants : 1,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.ActivityListings.Add(listing);
        await _db.SaveChangesAsync();

        return Created($"/api/catalog/activity-listings/{listing.Id}", listing);
    }

    // Get provider's own listings
    [HttpGet]
    public async Task<IActionResult> GetMyListings()
    {
        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
        {
            return StatusCode(403, new { message = errorMessage });
        }

        var listings = await _db.ActivityListings
            .AsNoTracking()
            .Where(l => l.ProviderId == provider.Id)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return Ok(listings);
    }

    
    // Get listing by Id 
    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
        {
            return StatusCode(403, new { message = errorMessage });
        }

        var listing = await _db.ActivityListings
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing is null)
            return NotFound(new { message = "Activity listing not found." });

        if (listing.ProviderId != provider.Id)
            return StatusCode(403, new { message = "You do not have permission to view this listing." });

        return Ok(listing);
    }

    // Update 
  
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateActivityListingRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
        {
            return StatusCode(403, new { message = errorMessage });
        }

        var listing = await _db.ActivityListings.FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null)
            return NotFound(new { message = "Activity listing not found." });

        // Ownership check
        if (listing.ProviderId != provider.Id)
            return StatusCode(403, new { message = "You can only update your own listings." });

        listing.Title = request.Title.Trim();
        listing.Description = request.Description.Trim();
        listing.Price = request.Price;
        listing.Unit = request.Unit.Trim();
        listing.Location = request.Location.Trim();
        listing.MaxParticipants = request.MaxParticipants > 0 ? request.MaxParticipants : 1;
        listing.IsActive = request.IsActive;

        await _db.SaveChangesAsync();

        return Ok(listing);
    }

    // CEYQ-85: Delete Own Listing (DELETE)
	
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
        {
            return StatusCode(403, new { message = errorMessage });
        }

        var listing = await _db.ActivityListings.FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null)
            return NotFound(new { message = "Activity listing not found." });

        // Ownership check
        if (listing.ProviderId != provider.Id)
            return StatusCode(403, new { message = "You can only delete your own listings." });

        _db.ActivityListings.Remove(listing);
        await _db.SaveChangesAsync();

        return NoContent(); // 204
    }

    // Helper (auto-links approved providers)
    private async Task<(Provider? provider, string? errorMessage)> GetApprovedProviderAsync()
    {
        var identityUserId = GetIdentityUserId();
        var email = User.FindFirstValue(ClaimTypes.Email) 
                 ?? User.FindFirstValue("email");

        if (identityUserId is null && string.IsNullOrWhiteSpace(email))
        {
            return (null, "User identity could not be verified from token.");
        }

        // Match either by linked IdentityUserId or by verified email
        var provider = await _db.Providers.FirstOrDefaultAsync(p =>
            (identityUserId.HasValue && p.IdentityUserId == identityUserId.Value) ||
            (!string.IsNullOrEmpty(email) && p.Email.ToLower() == email.ToLower()));

        if (provider is null)
        {
            return (null, "Only approved providers can manage experience listings. Your application may still be pending or was rejected.");
        }

        // Auto-link IdentityUserId if not already stored
        if (provider.IdentityUserId == null && identityUserId.HasValue)
        {
            provider.IdentityUserId = identityUserId.Value;
            await _db.SaveChangesAsync();
        }

        return (provider, null);
    }

    private Guid? GetIdentityUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");

        return Guid.TryParse(raw, out var id) ? id : null;
    }
}