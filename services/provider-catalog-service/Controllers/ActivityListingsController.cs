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

    
    // Create
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

    // Get My Listing
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

    
    // Get by Id
    
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

        return Ok(ToDto(listing));
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
        listing.Duration = request.Duration?.Trim() ?? listing.Duration;
        listing.AvailableDays = request.AvailableDays?.Trim() ?? "";
        listing.TimeSlots = request.TimeSlots ?? "[]";
        listing.ValidFrom = request.ValidFrom;
        listing.ValidUntil = request.ValidUntil;

        await _db.SaveChangesAsync();

        return Ok(ToDto(listing));
    }

    // Delete
	
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

    // Visitor activity listings
    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicListings(
        [FromQuery] string? search,
        [FromQuery] string? location,
        [FromQuery] decimal? maxPrice)
    {
        var query = _db.ActivityListings
            .Include(l => l.Provider)
            .AsNoTracking()
            .Where(l => l.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            query = query.Where(l => l.Title.ToLower().Contains(q) || l.Description.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var loc = location.Trim().ToLower();
            query = query.Where(l => l.Location.ToLower().Contains(loc));
        }

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
}