using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;

namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/accommodation-listings")]
[Authorize(Roles = "Provider")]
public class AccommodationListingsController : ControllerBase
{
    private readonly CatalogDbContext _db;

    public AccommodationListingsController(CatalogDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Maps an accommodation listing entity to the response DTO returned by the API
    /// </summary>
    private static AccommodationListingResponse ToDto(AccommodationListing a, string businessName = "") => new()
    {
        Id = a.Id,
        ProviderId = a.ProviderId,
        ProviderBusinessName = !string.IsNullOrEmpty(businessName) ? businessName : a.Provider?.BusinessName ?? "",
        RoomType = a.RoomType,
        PropertyType = a.PropertyType,
        Location = a.Location,
        PricePerNight = a.PricePerNight,
        MaxGuests = a.MaxGuests,
        BedDetails = a.BedDetails,
        MinStayNights = a.MinStayNights,
        Amenities = a.Amenities,
        BathroomDetails = a.BathroomDetails,
        Description = a.Description,
        IsActive = a.IsActive,
        CreatedAt = a.CreatedAt,
        Images = a.Images
    };

    /// <summary>
    /// Creates a new accommodation listing for the currently authenticated approved provider
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccommodationListingRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
            return StatusCode(403, new { message = errorMessage });

        var listing = new AccommodationListing
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            RoomType = request.RoomType.Trim(),
            PropertyType = request.PropertyType.Trim(),
            Location = request.Location.Trim(),
            PricePerNight = request.PricePerNight,
            MaxGuests = request.MaxGuests > 0 ? request.MaxGuests : 1,
            BedDetails = request.BedDetails.Trim(),
            MinStayNights = request.MinStayNights > 0 ? request.MinStayNights : 1,
            Amenities = request.Amenities?.Trim() ?? "",
            BathroomDetails = request.BathroomDetails?.Trim() ?? "",
            Description = request.Description.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            Images = request.Images
        };

        _db.AccommodationListings.Add(listing);
        await _db.SaveChangesAsync();

        return Created($"/api/catalog/accommodation-listings/{listing.Id}", ToDto(listing, provider.BusinessName));
    }

    /// <summary>
    /// Retrieves all accommodation listings belonging to the currently authenticated approved provider
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyListings()
    {
        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
            return StatusCode(403, new { message = errorMessage });

        var listings = await _db.AccommodationListings
            .AsNoTracking()
            .Where(a => a.ProviderId == provider.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => ToDto(a, provider.BusinessName))
            .ToListAsync();

        return Ok(listings);
    }

    /// <summary>
    /// Retrieves a specific accommodation listing after verifying that it belongs to the
    /// currently authenticated provider.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
            return StatusCode(403, new { message = errorMessage });

        var listing = await _db.AccommodationListings
            .AsNoTracking()
            .Include(a => a.Provider)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (listing is null)
            return NotFound(new { message = "Accommodation listing not found." });

        if (listing.ProviderId != provider.Id)
            return StatusCode(403, new { message = "You do not have permission to view this listing." });

        return Ok(ToDto(listing));
    }

    /// <summary>
    /// Updates an existing accommodation listing after verifying that the currently
    /// authenticated provider owns the listing.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAccommodationListingRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
            return StatusCode(403, new { message = errorMessage });

        var listing = await _db.AccommodationListings.FirstOrDefaultAsync(a => a.Id == id);
        if (listing is null)
            return NotFound(new { message = "Accommodation listing not found." });

        // Strict ownership check
        if (listing.ProviderId != provider.Id)
            return StatusCode(403, new { message = "You can only update your own accommodation listings." });

        listing.RoomType = request.RoomType.Trim();
        listing.PropertyType = request.PropertyType.Trim();
        listing.Location = request.Location.Trim();
        listing.PricePerNight = request.PricePerNight;
        listing.MaxGuests = request.MaxGuests > 0 ? request.MaxGuests : 1;
        listing.BedDetails = request.BedDetails.Trim();
        listing.MinStayNights = request.MinStayNights > 0 ? request.MinStayNights : 1;
        listing.Amenities = request.Amenities?.Trim() ?? "";
        listing.BathroomDetails = request.BathroomDetails?.Trim() ?? "";
        listing.Description = request.Description.Trim();
        listing.IsActive = request.IsActive;
        listing.Images = request.Images;

        await _db.SaveChangesAsync();

        return Ok(ToDto(listing, provider.BusinessName));
    }

    /// <summary>
    /// Deletes an accommodation listing after verifying that the currently authenticated
    /// provider owns the listing.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
            return StatusCode(403, new { message = errorMessage });

        var listing = await _db.AccommodationListings.FirstOrDefaultAsync(a => a.Id == id);
        if (listing is null)
            return NotFound(new { message = "Accommodation listing not found." });

        // Strict ownership check
        if (listing.ProviderId != provider.Id)
            return StatusCode(403, new { message = "You can only delete your own accommodation listings." });

        _db.AccommodationListings.Remove(listing);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Retrieves active accommodation listings for public discovery, with optional
    /// filtering by search term, property type, location, and guest capacity.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicListings(
        [FromQuery] string? search,
        [FromQuery] string? propertyType,
        [FromQuery] string? location,
        [FromQuery] int? guests)
    {
        // Only active listings are shown in public discovery
        var query = _db.AccommodationListings
            .AsNoTracking()
            .Include(a => a.Provider)
            .Where(a => a.IsActive);

        // Search across the main listing details
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower().Trim();
            query = query.Where(a =>
                a.RoomType.ToLower().Contains(s) ||
                a.Description.ToLower().Contains(s) ||
                a.PropertyType.ToLower().Contains(s) ||
                a.Location.ToLower().Contains(s) ||
                a.Amenities.ToLower().Contains(s));
        }

        // Filter by property type when provided
        if (!string.IsNullOrWhiteSpace(propertyType))
        {
            var p = propertyType.ToLower().Trim();
            query = query.Where(a => a.PropertyType.ToLower().Contains(p));
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var loc = location.ToLower().Trim();
            query = query.Where(a => a.Location.ToLower().Contains(loc));
        }

        if (guests.HasValue && guests.Value > 0)
        {
            query = query.Where(a => a.MaxGuests >= guests.Value);
        }

        var results = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => ToDto(a, a.Provider != null ? a.Provider.BusinessName : ""))
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>
    /// Identifies the authenticated provider and verifies that the provider is approved
    /// before allowing accommodation listing management
    /// </summary>
    private async Task<(Provider? provider, string? errorMessage)> GetApprovedProviderAsync()
    {
        var identityUserId = GetIdentityUserId();
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

        // A user must have an identifiable claim to be matched with a provider
        if (identityUserId is null && string.IsNullOrWhiteSpace(email))
            return (null, "User identity could not be verified from token.");

        // Match the provider using the identity user ID or email from the token
        var provider = await _db.Providers.FirstOrDefaultAsync(p =>
            (identityUserId.HasValue && p.IdentityUserId == identityUserId.Value) ||
            (!string.IsNullOrEmpty(email) && p.Email.ToLower() == email.ToLower()));

        if (provider is null)
            return (null, "Only approved providers can manage accommodation listings. Your application may still be pending or was rejected.");

        // Link the provider to the identity user if the relationship has not been stored yet
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
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}