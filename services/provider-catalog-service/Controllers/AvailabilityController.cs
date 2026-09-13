using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Services;

namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/availability")]
public class AvailabilityController : ControllerBase
{
    private readonly AvailabilityService _availabilityService;

    public AvailabilityController(AvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    // ── 1. GET Availability for Date (Public / Visitor & Provider) ───────────
    [HttpGet("{listingId:guid}")]
    public async Task<IActionResult> GetAvailability(Guid listingId, [FromQuery] string date)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
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

    // ── 2. PUT Set Slot Capacity (Scenario 1 & Scenario 2) ───────────────────
    [HttpPut("{listingId:guid}")]
    [Authorize(Roles = "Provider")]
    public async Task<IActionResult> SetAvailability(Guid listingId, [FromBody] SetAvailabilityRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (request.Capacity <= 0)
            return BadRequest(new { message = "Capacity must be greater than 0." });

        if (!DateOnly.TryParse(request.Date, out var parsedDate))
            return BadRequest(new { message = "Invalid date format. Expected YYYY-MM-DD." });

        await _availabilityService.SetSlotCapacityAsync(listingId, parsedDate, request.TimeSlot, request.Capacity);

        return Ok(new { message = "Availability capacity updated successfully." });
    }

    // ── 3. POST Simulate Booking Event (Dev / Swagger simulation) ────────────
    [HttpPost("simulate-booking-event")]
    public async Task<IActionResult> SimulateBooking([FromBody] SimulateBookingEventRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!DateOnly.TryParse(request.Date, out var parsedDate))
            return BadRequest(new { message = "Invalid date format. Expected YYYY-MM-DD." });

        await _availabilityService.DeductCapacityAsync(request.ListingId, parsedDate, request.TimeSlot, request.GuestCount);

        var updated = await _availabilityService.GetAvailabilityForDateAsync(request.ListingId, parsedDate);
        return Ok(new { message = "Booking event simulated and capacity deducted.", availability = updated });
    }
}