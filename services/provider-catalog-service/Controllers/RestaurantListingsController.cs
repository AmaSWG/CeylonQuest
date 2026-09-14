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
[Route("api/catalog/restaurant-listings")]
[Authorize(Roles = "Provider")]
public class RestaurantListingsController : ControllerBase
{
    private readonly CatalogDbContext _db;

    public RestaurantListingsController(CatalogDbContext db)
    {
        _db = db;
    }

    private static RestaurantListingResponse ToDto(RestaurantListing r, string businessName = "") => new()
    {
        Id = r.Id,
        ProviderId = r.ProviderId,
        ProviderBusinessName = !string.IsNullOrEmpty(businessName) ? businessName : r.Provider?.BusinessName ?? "",
        Name = r.Name,
        Description = r.Description,
        CuisineType = r.CuisineType,
        DiningStyle = r.DiningStyle,
        Location = r.Location,
        PricePerPerson = r.PricePerPerson,
        PriceRange = r.PriceRange,
        OpeningHours = r.OpeningHours,
        SetMenuDetails = r.SetMenuDetails,
        DietaryOptions = r.DietaryOptions,
        GroupSizeCategory = r.GroupSizeCategory,
        SeatingCapacity = r.SeatingCapacity,
        IsActive = r.IsActive,
        CreatedAt = r.CreatedAt
    };

    /// <summary>
    /// Creates a new restaurant listing owned by the authenticated approved provider
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRestaurantListingRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
            return StatusCode(403, new { message = errorMessage });

        var listing = new RestaurantListing
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            CuisineType = request.CuisineType.Trim(),
            DiningStyle = request.DiningStyle.Trim(),
            Location = request.Location.Trim(),
            PricePerPerson = request.PricePerPerson,
            PriceRange = request.PriceRange.Trim(),
            OpeningHours = request.OpeningHours.Trim(),
            SetMenuDetails = request.SetMenuDetails.Trim(),
            DietaryOptions = request.DietaryOptions.Trim(),
            GroupSizeCategory = request.GroupSizeCategory?.Trim() ?? "Table for Two",
            SeatingCapacity = request.SeatingCapacity > 0 ? request.SeatingCapacity : 1,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.RestaurantListings.Add(listing);
        await _db.SaveChangesAsync();

        return Created($"/api/catalog/restaurant-listings/{listing.Id}", ToDto(listing, provider.BusinessName));
    }

    /// <summary>
    /// Retrieves all restaurant listings owned by the authenticated approved provider
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyListings()
    {
        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
            return StatusCode(403, new { message = errorMessage });

        var listings = await _db.RestaurantListings
            .AsNoTracking()
            .Where(r => r.ProviderId == provider.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToDto(r, provider.BusinessName))
            .ToListAsync();

        return Ok(listings);
    }

    /// <summary>
    /// Retrieves a single restaurant listing by ID, enforcing that it
    /// belongs to the authenticated approved provider
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
            return StatusCode(403, new { message = errorMessage });

        var listing = await _db.RestaurantListings
            .AsNoTracking()
            .Include(r => r.Provider)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (listing is null)
            return NotFound(new { message = "Restaurant listing not found." });

        // Ensure the provider only accesses their own listing
        if (listing.ProviderId != provider.Id)
            return StatusCode(403, new { message = "You do not have permission to view this listing." });

        return Ok(ToDto(listing));
    }

    /// <summary>
    /// Updates a restaurant listing owned by the authenticated approved provider
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRestaurantListingRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
            return StatusCode(403, new { message = errorMessage });

        var listing = await _db.RestaurantListings.FirstOrDefaultAsync(r => r.Id == id);
        if (listing is null)
            return NotFound(new { message = "Restaurant listing not found." });

        // Strict ownership check
        if (listing.ProviderId != provider.Id)
            return StatusCode(403, new { message = "You can only update your own restaurant listings." });

        listing.Name = request.Name.Trim();
        listing.Description = request.Description.Trim();
        listing.CuisineType = request.CuisineType.Trim();
        listing.DiningStyle = request.DiningStyle.Trim();
        listing.Location = request.Location.Trim();
        listing.PricePerPerson = request.PricePerPerson;
        listing.PriceRange = request.PriceRange.Trim();
        listing.OpeningHours = request.OpeningHours.Trim();
        listing.SetMenuDetails = request.SetMenuDetails.Trim();
        listing.DietaryOptions = request.DietaryOptions.Trim();
        listing.GroupSizeCategory = request.GroupSizeCategory?.Trim() ?? listing.GroupSizeCategory;
        listing.SeatingCapacity = request.SeatingCapacity > 0 ? request.SeatingCapacity : 1;
        listing.IsActive = request.IsActive;

        await _db.SaveChangesAsync();

        return Ok(ToDto(listing, provider.BusinessName));
    }

    /// <summary>
    /// Deletes a restaurant listing owned by the authenticated approved provider
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
            return StatusCode(403, new { message = errorMessage });

        var listing = await _db.RestaurantListings.FirstOrDefaultAsync(r => r.Id == id);
        if (listing is null)
            return NotFound(new { message = "Restaurant listing not found." });

        // Strict ownership check
        if (listing.ProviderId != provider.Id)
            return StatusCode(403, new { message = "You can only delete your own restaurant listings." });

        _db.RestaurantListings.Remove(listing);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Returns active restaurant listings for public discovery, with optional
    /// filtering by search term, cuisine, and location
    /// </summary>
    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicListings(
        [FromQuery] string? search,
        [FromQuery] string? cuisine,
        [FromQuery] string? location)
    {
        var query = _db.RestaurantListings
            .AsNoTracking()
            .Include(r => r.Provider)
            .Where(r => r.IsActive);

        // Apply the free-text search across multiple listing fields
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower().Trim();
            query = query.Where(r =>
                r.Name.ToLower().Contains(s) ||
                r.Description.ToLower().Contains(s) ||
                r.CuisineType.ToLower().Contains(s) ||
                r.Location.ToLower().Contains(s) ||
                r.SetMenuDetails.ToLower().Contains(s));
        }

        // Filter by cuisine type when provided
        if (!string.IsNullOrWhiteSpace(cuisine))
        {
            var c = cuisine.ToLower().Trim();
            query = query.Where(r => r.CuisineType.ToLower().Contains(c));
        }

        // Filter by location when provided
        if (!string.IsNullOrWhiteSpace(location))
        {
            var loc = location.ToLower().Trim();
            query = query.Where(r => r.Location.ToLower().Contains(loc));
        }

        var results = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToDto(r, r.Provider != null ? r.Provider.BusinessName : ""))
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>
    /// Identifies the authenticated provider and verifies that the provider
    /// profile exists before allowing provider-specific listing management
    /// </summary>
    private async Task<(Provider? provider, string? errorMessage)> GetApprovedProviderAsync()
    {
        var identityUserId = GetIdentityUserId();
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

        // A valid identity ID or email is required to identify the provider
        if (identityUserId is null && string.IsNullOrWhiteSpace(email))
            return (null, "User identity could not be verified from token.");

        var provider = await _db.Providers.FirstOrDefaultAsync(p =>
            (identityUserId.HasValue && p.IdentityUserId == identityUserId.Value) ||
            (!string.IsNullOrEmpty(email) && p.Email.ToLower() == email.ToLower()));

        if (provider is null)
            return (null, "Only approved providers can manage restaurant listings. Your application may still be pending or was rejected.");

        if (provider.IdentityUserId == null && identityUserId.HasValue)
        {
            provider.IdentityUserId = identityUserId.Value;
            await _db.SaveChangesAsync();
        }

        return (provider, null);
    }

    /// <summary>
    /// Extracts the authenticated user's identity ID from the available
    /// NameIdentifier or subject token claim
    /// </summary>
    private Guid? GetIdentityUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}