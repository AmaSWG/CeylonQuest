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

    /// <summary>
    /// Maps an activity listing entity to the response DTO returned by the API
    /// </summary>
     private static ActivityListingResponse ToDto(ActivityListing a) => new()
    {
        Id = a.Id,
        Title = a.Title,
        Description = a.Description,
        Price = a.Price,
        Unit = a.Unit,
        Location = a.Location,
        MaxParticipants = a.MaxParticipants,
        IsActive = a.IsActive,
        CreatedAt = a.CreatedAt,
        Duration = a.Duration,
        AvailableDays = a.AvailableDays,
        TimeSlots = a.TimeSlots,
        ValidFrom = a.ValidFrom,
        ValidUntil = a.ValidUntil
    };

    
    /// <summary>
    /// Creates a new activity listing for the currently authenticated approved provider
    /// </summary>
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
            CreatedAt = DateTime.UtcNow,
            Duration = request.Duration?.Trim(),
            AvailableDays = request.AvailableDays?.Trim(),
            TimeSlots = request.TimeSlots?.Trim(),
            ValidFrom = request.ValidFrom,
            ValidUntil = request.ValidUntil
        };

        _db.ActivityListings.Add(listing);
        await _db.SaveChangesAsync();

        return Created($"/api/catalog/activity-listings/{listing.Id}", ToDto(listing));
    }

    /// <summary>
    /// Retrieves all activity listings belonging to the currently authenticated approved provider
    /// </summary
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
            .Select(l => new ActivityListingResponse
            {
                Id = l.Id,
                Title = l.Title,
                Description = l.Description,
                Price = l.Price,
                Unit = l.Unit,
                Location = l.Location,
                MaxParticipants = l.MaxParticipants,
                IsActive = l.IsActive,
                CreatedAt = l.CreatedAt,
                Duration = l.Duration,
                AvailableDays = l.AvailableDays,
                TimeSlots = l.TimeSlots,
                ValidFrom = l.ValidFrom,
                ValidUntil = l.ValidUntil
            })
            .ToListAsync();

        return Ok(listings);
    }

    /// <summary>
    /// Retrieves a specific activity listing after verifying that it belongs to the
    /// currently authenticated provider
    /// </summary>
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

        // Providers can only access their own listings
        if (listing.ProviderId != provider.Id)
            return StatusCode(403, new { message = "You do not have permission to view this listing." });

        return Ok(ToDto(listing));
    }


    /// <summary>
    /// Updates an existing activity listing after verifying that the currently
    /// authenticated provider owns the listing
    /// </summary>
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

        // Ownership is checked before allowing the listing to be modified
        if (listing.ProviderId != provider.Id)
            return StatusCode(403, new { message = "You can only update your own listings." });

        listing.Title = request.Title.Trim();
        listing.Description = request.Description.Trim();
        listing.Price = request.Price;
        listing.Unit = request.Unit.Trim();
        listing.Location = request.Location.Trim();
        listing.MaxParticipants = request.MaxParticipants > 0 ? request.MaxParticipants : 1;
        listing.IsActive = request.IsActive;
        listing.Duration = request.Duration?.Trim() ?? listing.Duration;
        listing.AvailableDays = !string.IsNullOrWhiteSpace(request.AvailableDays)
            ? request.AvailableDays.Trim()
            : listing.AvailableDays;
        listing.TimeSlots = request.TimeSlots ?? "[]";
        listing.ValidFrom = request.ValidFrom;
        listing.ValidUntil = request.ValidUntil;

        await _db.SaveChangesAsync();

        return Ok(ToDto(listing));
    }

	/// <summary>
	/// Deletes an activity listing after verifying that the currently authenticated
	/// provider owns the listing
	/// </summary>
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

        // Ownership is checked before the listing can be deleted
        if (listing.ProviderId != provider.Id)
            return StatusCode(403, new { message = "You can only delete your own listings." });

        _db.ActivityListings.Remove(listing);
        await _db.SaveChangesAsync();

        return NoContent(); // 204
    }

    /// <summary>
    /// Identifies the authenticated provider and verifies that the provider is approved
    /// before allowing activity listing management
    /// </summary>
    private async Task<(Provider? provider, string? errorMessage)> GetApprovedProviderAsync()
    {
        var identityUserId = GetIdentityUserId();
        var email = User.FindFirstValue(ClaimTypes.Email) 
                 ?? User.FindFirstValue("email");

        // A user must have an identifiable claim to be matched with a provider
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

        // Link the provider to the identity user if it has not been stored yet
        if (provider.IdentityUserId == null && identityUserId.HasValue)
        {
            provider.IdentityUserId = identityUserId.Value;
            await _db.SaveChangesAsync();
        }

        return (provider, null);
    }

    /// <summary>
    /// Extracts the authenticated user's identity ID from the available token claims
    /// </summary>
    private Guid? GetIdentityUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");

        return Guid.TryParse(raw, out var id) ? id : null;
    }

    /// <summary>
    /// Retrieves active activity listings for public discovery, with optional
    /// filtering by search term, location, and maximum price
    /// </summary>
    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicListings(
        [FromQuery] string? search,
        [FromQuery] string? location,
        [FromQuery] decimal? maxPrice)
    {
        // Only active listings are included in public results
        var query = _db.ActivityListings
            .Include(l => l.Provider)
            .AsNoTracking()
            .Where(l => l.IsActive);

        // Search across the activity title and description
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            query = query.Where(l => l.Title.ToLower().Contains(q) || l.Description.ToLower().Contains(q));
        }

        // Filter activities by location when provided
        if (!string.IsNullOrWhiteSpace(location))
        {
            var loc = location.Trim().ToLower();
            query = query.Where(l => l.Location.ToLower().Contains(loc));
        }

        // Return activities within the visitor's maximum price
        if (maxPrice.HasValue && maxPrice > 0)
        {
            query = query.Where(l => l.Price <= maxPrice.Value);
        }

        var listings = await query
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                l.Id,
                l.Title,
                l.Description,
                l.Price,
                l.Unit,
                l.Location,
                l.MaxParticipants,
                l.CreatedAt,
                l.Duration,
                l.AvailableDays,
                l.TimeSlots,
                l.ValidFrom,
                l.ValidUntil,
                ProviderBusinessName = l.Provider.BusinessName,
                ProviderEmail = l.Provider.Email
            })
            .ToListAsync();

        return Ok(listings);
    }

    //I am adding a comment here because I want the pipeline to run because it decided to be a little B* during previous commit
    //I am adding yet another comment here because I want the pipeline to run because it decided to be a little B* during the previous commit too
}