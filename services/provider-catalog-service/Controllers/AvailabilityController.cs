using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;
using ProviderCatalogService.Services;

namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/availability")]
public class AvailabilityController : ControllerBase
{
    private readonly AvailabilityService _availabilityService;
    private readonly CatalogDbContext _db;

    public AvailabilityController(AvailabilityService availabilityService, CatalogDbContext db)
    {
        _availabilityService = availabilityService;
        _db = db;
    }

    // ── 1. GET Availability for Date (Public / Visitor & Provider) ───────────
    [HttpGet("{listingId:guid}")]
    public async Task<IActionResult> GetAvailability(Guid listingId, [FromQuery] string date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return BadRequest(new { message = "Invalid date format. Expected YYYY-MM-DD." });
        }

        var result = await _availabilityService.GetAvailabilityForDateAsync(listingId, parsedDate);
        if (result == null)
        {
            return NotFound(new { message = "Listing not found or is inactive." });
        }

        return Ok(result);
    }

    // ── 2. PUT Set Slot Capacity (Provider Management) ───────────────────────
    [HttpPut("{listingId:guid}")]
    [Authorize(Roles = "Provider")]
    public async Task<IActionResult> SetAvailability(Guid listingId, [FromBody] SetAvailabilityRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (request.Capacity <= 0)
            return BadRequest(new { message = "Capacity must be greater than 0." });

        // TC60-13: Validate date format & reject past dates
        if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return BadRequest(new { message = "Invalid date format. Expected YYYY-MM-DD." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (parsedDate < today)
        {
            return BadRequest(new { message = "Cannot create or modify availability for past dates." });
        }

        // TC60-12: Resolve logged-in provider and check listing ownership
        var (provider, errorMessage) = await GetApprovedProviderAsync();
        if (provider is null)
        {
            return StatusCode(403, new { message = errorMessage });
        }

        var (listingFound, ownerId) = await GetListingOwnerAsync(listingId);
        if (!listingFound)
        {
            return NotFound(new { message = "Listing not found." });
        }

        if (ownerId != provider.Id)
        {
            return StatusCode(403, new { message = "You do not have permission to modify availability for this listing." });
        }

        try
        {
            // TC60-15: Will throw InvalidOperationException if capacity < bookedCount
            await _availabilityService.SetSlotCapacityAsync(listingId, parsedDate, request.TimeSlot, request.Capacity);
            return Ok(new { message = "Availability capacity updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── 3. POST Simulate Booking Event (Dev / Swagger simulation) ────────────
    [HttpPost("simulate-booking-event")]
    public async Task<IActionResult> SimulateBooking([FromBody] SimulateBookingEventRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return BadRequest(new { message = "Invalid date format. Expected YYYY-MM-DD." });

        await _availabilityService.DeductCapacityAsync(request.ListingId, parsedDate, request.TimeSlot, request.GuestCount);

        var updated = await _availabilityService.GetAvailabilityForDateAsync(request.ListingId, parsedDate);
        return Ok(new { message = "Booking event simulated and capacity deducted.", availability = updated });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private async Task<(bool found, Guid? ownerId)> GetListingOwnerAsync(Guid listingId)
    {
        var act = await _db.ActivityListings.AsNoTracking().FirstOrDefaultAsync(a => a.Id == listingId);
        if (act != null) return (true, act.ProviderId);

        var rest = await _db.RestaurantListings.AsNoTracking().FirstOrDefaultAsync(r => r.Id == listingId);
        if (rest != null) return (true, rest.ProviderId);

        var stay = await _db.AccommodationListings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == listingId);
        if (stay != null) return (true, stay.ProviderId);

        return (false, null);
    }

    private async Task<(Provider? provider, string? errorMessage)> GetApprovedProviderAsync()
    {
        var identityUserId = GetIdentityUserId();
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

        if (identityUserId is null && string.IsNullOrWhiteSpace(email))
            return (null, "User identity could not be verified from token.");

        var provider = await _db.Providers.FirstOrDefaultAsync(p =>
            (identityUserId.HasValue && p.IdentityUserId == identityUserId.Value) ||
            (!string.IsNullOrEmpty(email) && p.Email.ToLower() == email.ToLower()));

        if (provider is null)
            return (null, "Provider profile not found.");

        return (provider, null);
    }

    private Guid? GetIdentityUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}