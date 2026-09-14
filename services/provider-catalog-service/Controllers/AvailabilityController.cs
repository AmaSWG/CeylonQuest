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

    /// <summary>
    /// Retrieves the availability of a listing for a specific date
    /// </summary>
    [HttpGet("{listingId:guid}")]
    public async Task<IActionResult> GetAvailability(Guid listingId, [FromQuery] string date)
    {
        // Validate the date using the required YYYY-MM-DD format
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

    /// <summary>
    /// Sets or updates the capacity of a specific availability time slot
    /// for a listing owned by the authenticated approved provider
    /// </summary>
    [HttpPut("{listingId:guid}")]
    [Authorize(Roles = "Provider")]
    public async Task<IActionResult> SetAvailability(Guid listingId, [FromBody] SetAvailabilityRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (request.Capacity <= 0)
            return BadRequest(new { message = "Capacity must be greater than 0." });

        // Validate the date format and reject availability changes for past dates
        if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return BadRequest(new { message = "Invalid date format. Expected YYYY-MM-DD." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (parsedDate < today)
        {
            return BadRequest(new { message = "Cannot create or modify availability for past dates." });
        }

        // Validate the date format and reject availability changes for past dates
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

        // Providers can only modify availability for their own listings
        if (ownerId != provider.Id)
        {
            return StatusCode(403, new { message = "You do not have permission to modify availability for this listing." });
        }

        try
        {
            // The service prevents capacity from being set below the booked count
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

    /// <summary>
    /// Simulates a booking event by deducting the requested guest count
    /// from the listing's availability
    /// </summary>
    [HttpPost("simulate-booking-event")]
    public async Task<IActionResult> SimulateBooking([FromBody] SimulateBookingEventRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // Validate the booking date before updating availability
        if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return BadRequest(new { message = "Invalid date format. Expected YYYY-MM-DD." });

        await _availabilityService.DeductCapacityAsync(request.ListingId, parsedDate, request.TimeSlot, request.GuestCount);

        var updated = await _availabilityService.GetAvailabilityForDateAsync(request.ListingId, parsedDate);
        return Ok(new { message = "Booking event simulated and capacity deducted.", availability = updated });
    }

    /// <summary>
    /// Simulates a booking cancellation event by restoring the requested
    /// guest count to the listing's availability.
    /// </summary>
    [HttpPost("simulate-booking-canceled-event")]
    public async Task<IActionResult> SimulateBookingCanceled([FromBody] SimulateBookingCanceledEventRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // Validate the cancellation date before restoring availability
        if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return BadRequest(new { message = "Invalid date format. Expected YYYY-MM-DD." });

        await _availabilityService.RestoreCapacityAsync(request.ListingId, parsedDate, request.TimeSlot, request.GuestCount);

        var updated = await _availabilityService.GetAvailabilityForDateAsync(request.ListingId, parsedDate);
        return Ok(new { message = "Booking cancellation simulated and capacity restored.", availability = updated });
    }

    /// <summary>
    /// Simulates an updated booking event by adjusting availability between
    /// the original and updated booking dates, time slots, and guest counts
    /// </summary>
    [HttpPost("simulate-booking-updated-event")]
    public async Task<IActionResult> SimulateBookingUpdated([FromBody] SimulateBookingUpdatedEventRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // Validate both the original and updated booking dates
        if (!DateOnly.TryParseExact(request.OldDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedOldDate))
            return BadRequest(new { message = "Invalid OldDate format. Expected YYYY-MM-DD." });

        if (!DateOnly.TryParseExact(request.NewDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedNewDate))
            return BadRequest(new { message = "Invalid NewDate format. Expected YYYY-MM-DD." });

        await _availabilityService.UpdateCapacityAsync(
            request.ListingId,
            parsedOldDate,
            request.OldTimeSlot,
            request.OldGuestCount,
            parsedNewDate,
            request.NewTimeSlot,
            request.NewGuestCount);

        var updatedOld = await _availabilityService.GetAvailabilityForDateAsync(request.ListingId, parsedOldDate);
        var updatedNew = await _availabilityService.GetAvailabilityForDateAsync(request.ListingId, parsedNewDate);
        return Ok(new { message = "Booking update simulated and capacity adjusted.", oldDateAvailability = updatedOld, newDateAvailability = updatedNew });
    }

    /// <summary>
    /// Finds the owner of a listing by checking the supported activity,
    /// restaurant, and accommodation listing types
    /// </summary>
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

    /// <summary>
    /// Identifies the authenticated provider and verifies that the provider
    /// profile exists before allowing provider-specific availability management
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
            return (null, "Provider profile not found.");

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