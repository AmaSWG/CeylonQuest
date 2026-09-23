using BookingService.Data;
using BookingService.DTOs;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly BookingDbContext _context;
    private readonly ICatalogService _catalogService;

    public ReservationsController(
        BookingDbContext context,
        ICatalogService catalogService)
    {
        _context = context;
        _catalogService = catalogService;
    }

    // POST: /api/reservations
    [HttpPost]
    public async Task<IActionResult> CreateReservation(
        [FromBody] CreateRestaurantReservationRequest request)
    {
        // 1. Get logged-in visitor ID from JWT
        var visitorIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(visitorIdValue) ||
            !Guid.TryParse(visitorIdValue, out var visitorId))
        {
            return Unauthorized(new
            {
                message = "Invalid visitor authentication."
            });
        }

        // 2. Validate restaurant ID
        if (request.RestaurantId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A valid restaurant is required."
            });
        }

        // 3. Validate reservation date
        if (request.ReservationDate <
            DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new
            {
                message = "Reservation date cannot be in the past."
            });
        }

        // 4. Validate time slot
        if (string.IsNullOrWhiteSpace(request.TimeSlot))
        {
            return BadRequest(new
            {
                message = "A reservation time is required."
            });
        }

        // 5. Validate party size
        if (request.PartySize <= 0)
        {
            return BadRequest(new
            {
                message = "Party size must be at least 1."
            });
        }

        // 6. Get restaurant details from Provider Catalog
        var restaurant =
            await _catalogService.GetRestaurantAsync(
                request.RestaurantId);

        if (restaurant == null)
        {
            return BadRequest(new
            {
                message = "Restaurant could not be found."
            });
        }

        // 7. Check whether restaurant is active
        if (!restaurant.IsActive)
        {
            return BadRequest(new
            {
                message =
                    "This restaurant is currently unavailable."
            });
        }

        // 8. Validate party size against restaurant seating capacity
        if (request.PartySize > restaurant.SeatingCapacity)
        {
            return BadRequest(new
            {
                message =
                    $"Maximum seating capacity is {restaurant.SeatingCapacity}."
            });
        }

        // 9. Retrieve restaurant availability
        var availability =
            await _catalogService.GetAvailabilityAsync(
                request.RestaurantId,
                request.ReservationDate);

        if (availability == null)
        {
            return BadRequest(new
            {
                message =
                    "Could not retrieve restaurant availability."
            });
        }

        // 10. Check whether restaurant operates on selected date
        if (!availability.IsOperatingDay)
        {
            return BadRequest(new
            {
                message =
                    "The restaurant is not available on the selected date."
            });
        }

        // 11. Check whether restaurant is fully booked
        if (availability.IsFullyBooked)
        {
            return BadRequest(new
            {
                message =
                    "The restaurant is fully booked on the selected date."
            });
        }

        // 12. Find selected reservation time
        var selectedSlot =
            availability.Slots.FirstOrDefault(slot =>
                string.Equals(
                    slot.TimeSlot,
                    request.TimeSlot,
                    StringComparison.OrdinalIgnoreCase));

        if (selectedSlot == null)
        {
            return BadRequest(new
            {
                message =
                    "The selected reservation time is not available."
            });
        }

        // 13. Check remaining capacity
        if (selectedSlot.IsFullyBooked ||
            request.PartySize > selectedSlot.RemainingCapacity)
        {
            return BadRequest(new
            {
                message =
                    $"Only {selectedSlot.RemainingCapacity} seats are available for this time."
            });
        }

        // 14. Reserve capacity immediately before saving
        // This helps prevent overbooking.
        var capacityReserved =
            await _catalogService.ReserveCapacityAsync(
                request.RestaurantId,
                request.ReservationDate,
                request.TimeSlot,
                request.PartySize);

        if (!capacityReserved)
        {
            return Conflict(new
            {
                message =
                    "The selected seats are no longer available. Please check availability and try again."
            });
        }

        // 15. Create restaurant reservation
        var reservation = new RestaurantReservation
        {
            Id = Guid.NewGuid(),
            VisitorId = visitorId,
            RestaurantId = request.RestaurantId,
            RestaurantName = restaurant.Name,
            ReservationDate = request.ReservationDate,
            TimeSlot = request.TimeSlot,
            PartySize = request.PartySize,
            Status = ReservationStatus.Confirmed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 16. Save reservation to database
        _context.RestaurantReservations.Add(reservation);

        await _context.SaveChangesAsync();

        // 17. Create reservation response
        var response = new RestaurantReservationResponse
        {
            Id = reservation.Id,
            RestaurantId = reservation.RestaurantId,
            RestaurantName = reservation.RestaurantName,
            ReservationDate = reservation.ReservationDate,
            TimeSlot = reservation.TimeSlot,
            PartySize = reservation.PartySize,
            Status = reservation.Status,
            CreatedAt = reservation.CreatedAt
        };

        // 18. Return reservation confirmation
        return Ok(response);
    }

    // GET: /api/reservations/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyReservations()
    {
        // 1. Get logged-in visitor ID from JWT
        var visitorIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(visitorIdValue) ||
            !Guid.TryParse(visitorIdValue, out var visitorId))
        {
            return Unauthorized(new
            {
                message = "Invalid visitor authentication."
            });
        }

        // 2. Get reservations belonging to this visitor
        var reservations =
            await _context.RestaurantReservations
                .AsNoTracking()
                .Where(r => r.VisitorId == visitorId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new RestaurantReservationResponse
                {
                    Id = r.Id,
                    RestaurantId = r.RestaurantId,
                    RestaurantName = r.RestaurantName,
                    ReservationDate = r.ReservationDate,
                    TimeSlot = r.TimeSlot,
                    PartySize = r.PartySize,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

        return Ok(reservations);
    }
}