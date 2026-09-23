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
        // 1. Get logged-in visitor ID
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

        // 2. Validate restaurant
        if (request.RestaurantId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A valid restaurant is required."
            });
        }

        // 3. Validate date
        if (request.ReservationDate <
            DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new
            {
                message = "Reservation date cannot be in the past."
            });
        }

        // 4. Validate time
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

        // 6. Get trusted restaurant details from Provider Catalog
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

        // 7. Restaurant must be active
        if (!restaurant.IsActive)
        {
            return BadRequest(new
            {
                message =
                    "This restaurant is currently unavailable."
            });
        }

        // 8. Validate provider price
        if (restaurant.PricePerPerson <= 0)
        {
            return BadRequest(new
            {
                message =
                    "This restaurant does not have a valid price per person."
            });
        }

        // 9. Validate maximum seating
        if (request.PartySize > restaurant.SeatingCapacity)
        {
            return BadRequest(new
            {
                message =
                    $"Maximum seating capacity is {restaurant.SeatingCapacity}."
            });
        }

        // 10. Get availability
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

        // 11. Check operating date
        if (!availability.IsOperatingDay)
        {
            return BadRequest(new
            {
                message =
                    "The restaurant is not available on the selected date."
            });
        }

        // 12. Check fully booked
        if (availability.IsFullyBooked)
        {
            return BadRequest(new
            {
                message =
                    "The restaurant is fully booked on the selected date."
            });
        }

        // 13. Find selected slot
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

        // 14. Check slot capacity
        if (selectedSlot.IsFullyBooked ||
            request.PartySize > selectedSlot.RemainingCapacity)
        {
            return BadRequest(new
            {
                message =
                    $"Only {selectedSlot.RemainingCapacity} seats are available for this time."
            });
        }

        // 15. Calculate price on backend.
        // Never trust a total supplied by the frontend.
        var pricePerPerson = restaurant.PricePerPerson;

        var totalPrice =
            pricePerPerson * request.PartySize;

        // 16. Reserve capacity
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

        // 17. Create reservation
        var reservation = new RestaurantReservation
        {
            Id = Guid.NewGuid(),
            VisitorId = visitorId,

            RestaurantId = request.RestaurantId,
            RestaurantName = restaurant.Name,

            ReservationDate = request.ReservationDate,
            TimeSlot = request.TimeSlot,

            PartySize = request.PartySize,

            PricePerPerson = pricePerPerson,
            TotalPrice = totalPrice,

            Status = ReservationStatus.Confirmed,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 18. Save
        _context.RestaurantReservations.Add(reservation);
        await _context.SaveChangesAsync();

        // 19. Response
        var response = new RestaurantReservationResponse
        {
            Id = reservation.Id,

            RestaurantId = reservation.RestaurantId,
            RestaurantName = reservation.RestaurantName,

            ReservationDate = reservation.ReservationDate,
            TimeSlot = reservation.TimeSlot,

            PartySize = reservation.PartySize,

            PricePerPerson = reservation.PricePerPerson,
            TotalPrice = reservation.TotalPrice,

            Status = reservation.Status,

            CreatedAt = reservation.CreatedAt
        };

        return Ok(response);
    }

    // GET: /api/reservations/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyReservations()
    {
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

                    PricePerPerson = r.PricePerPerson,
                    TotalPrice = r.TotalPrice,

                    Status = r.Status,

                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

        return Ok(reservations);
    }
}