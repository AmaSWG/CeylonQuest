using System.Security.Claims;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccommodationBookingsController : ControllerBase
{
    private readonly BookingDbContext _db;
    private readonly ICatalogService _catalogService;

    public AccommodationBookingsController(
        BookingDbContext db,
        ICatalogService catalogService)
    {
        _db = db;
        _catalogService = catalogService;
    }

    // =========================================================
    // CREATE ACCOMMODATION BOOKING
    // POST /api/AccommodationBookings
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAccommodationBookingRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // -----------------------------------------------------
        // 1. Get visitor ID from JWT
        // -----------------------------------------------------

        var visitorIdRaw =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!Guid.TryParse(visitorIdRaw, out var visitorId))
        {
            return Unauthorized(new
            {
                message = "Visitor identity could not be verified."
            });
        }

        // -----------------------------------------------------
        // 2. Basic validation
        // -----------------------------------------------------

        if (request.AccommodationId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "AccommodationId is required."
            });
        }

        var today = DateOnly.FromDateTime(DateTime.Now);

        if (request.CheckInDate < today)
        {
            return BadRequest(new
            {
                message = "Check-in date cannot be in the past."
            });
        }

        if (request.CheckOutDate <= request.CheckInDate)
        {
            return BadRequest(new
            {
                message = "Check-out date must be after the check-in date."
            });
        }

        if (request.GuestCount <= 0)
        {
            return BadRequest(new
            {
                message = "Guest count must be greater than 0."
            });
        }

        // -----------------------------------------------------
        // 3. Get accommodation from Provider Catalog
        // -----------------------------------------------------

        var accommodation =
            await _catalogService.GetAccommodationAsync(
                request.AccommodationId);

        if (accommodation == null)
        {
            return BadRequest(new
            {
                message = "Accommodation could not be found."
            });
        }

        if (!accommodation.IsActive)
        {
            return BadRequest(new
            {
                message = "This accommodation is currently unavailable."
            });
        }

        // -----------------------------------------------------
        // 4. Validate maximum guests
        // -----------------------------------------------------

        if (request.GuestCount > accommodation.MaxGuests)
        {
            return BadRequest(new
            {
                message =
                    $"This accommodation allows a maximum of " +
                    $"{accommodation.MaxGuests} guest(s)."
            });
        }

        // -----------------------------------------------------
        // 5. Calculate and validate number of nights
        // -----------------------------------------------------

        var numberOfNights =
            request.CheckOutDate.DayNumber -
            request.CheckInDate.DayNumber;

        if (numberOfNights < accommodation.MinStayNights)
        {
            return BadRequest(new
            {
                message =
                    $"This accommodation requires a minimum stay of " +
                    $"{accommodation.MinStayNights} night(s)."
            });
        }

        // -----------------------------------------------------
        // 6. Build the exact accommodation availability slot
        //
        // Provider Catalog creates:
        // Stay (Min 1 Night)
        // Stay (Min 2 Nights)
        // etc.
        // -----------------------------------------------------

        var slotName =
            $"Stay (Min {accommodation.MinStayNights} " +
            $"Night{(accommodation.MinStayNights > 1 ? "s" : "")})";

        // -----------------------------------------------------
        // 7. Check availability for check-in date
        // -----------------------------------------------------

        var availability =
            await _catalogService.GetAvailabilityAsync(
                request.AccommodationId,
                request.CheckInDate);

        if (availability == null)
        {
            return BadRequest(new
            {
                message =
                    "Accommodation availability could not be found " +
                    "for the selected check-in date."
            });
        }

        if (!availability.IsOperatingDay)
        {
            return BadRequest(new
            {
                message =
                    "This accommodation is not available on the selected date."
            });
        }

        if (availability.IsFullyBooked)
        {
            return BadRequest(new
            {
                message =
                    "This accommodation is fully booked on the selected date."
            });
        }

        var selectedSlot =
            availability.Slots?.FirstOrDefault(s =>
                string.Equals(
                    s.TimeSlot,
                    slotName,
                    StringComparison.OrdinalIgnoreCase));

        if (selectedSlot == null)
        {
            return BadRequest(new
            {
                message =
                    "Accommodation availability slot could not be found."
            });
        }

        if (selectedSlot.RemainingCapacity < 1)
        {
            return BadRequest(new
            {
                message =
                    "This accommodation is no longer available " +
                    "for the selected date."
            });
        }

        // -----------------------------------------------------
        // 8. Calculate trusted price on backend
        // -----------------------------------------------------

        var pricePerNight = accommodation.PricePerNight;

        var totalPrice =
            pricePerNight * numberOfNights;

        // -----------------------------------------------------
        // 9. Reserve ONE accommodation unit
        //
        // IMPORTANT:
        // GuestCount is NOT used here.
        //
        // AvailabilityService gives Accommodation default
        // capacity = 1.
        //
        // GuestCount is validated separately against MaxGuests.
        // -----------------------------------------------------

        var capacityReserved =
            await _catalogService.ReserveCapacityAsync(
                request.AccommodationId,
                request.CheckInDate,
                slotName,
                1);

        if (!capacityReserved)
        {
            return Conflict(new
            {
                message =
                    "This accommodation is no longer available " +
                    "for the selected date."
            });
        }

        // -----------------------------------------------------
        // 10. Create booking
        // -----------------------------------------------------

        var booking = new AccommodationBooking
        {
            Id = Guid.NewGuid(),

            VisitorId = visitorId,

            AccommodationId =
                accommodation.Id,

            AccommodationName =
                accommodation.RoomType,

            CheckInDate =
                request.CheckInDate,

            CheckOutDate =
                request.CheckOutDate,

            NumberOfNights =
                numberOfNights,

            GuestCount =
                request.GuestCount,

            PricePerNight =
                pricePerNight,

            TotalPrice =
                totalPrice,

            Status =
                AccommodationBookingStatus.Confirmed,

            CreatedAt =
                DateTime.UtcNow,

            UpdatedAt =
                DateTime.UtcNow
        };

        _db.AccommodationBookings.Add(booking);

        await _db.SaveChangesAsync();

        // -----------------------------------------------------
        // 11. Return created booking
        // -----------------------------------------------------

        var response =
            new AccommodationBookingResponse
            {
                Id = booking.Id,

                AccommodationId =
                    booking.AccommodationId,

                AccommodationName =
                    booking.AccommodationName,

                CheckInDate =
                    booking.CheckInDate,

                CheckOutDate =
                    booking.CheckOutDate,

                NumberOfNights =
                    booking.NumberOfNights,

                GuestCount =
                    booking.GuestCount,

                PricePerNight =
                    booking.PricePerNight,

                TotalPrice =
                    booking.TotalPrice,

                Status =
                    booking.Status,

                CreatedAt =
                    booking.CreatedAt
            };

        return CreatedAtAction(
            nameof(GetById),
            new { id = booking.Id },
            response);
    }


    // =========================================================
    // GET ONE ACCOMMODATION BOOKING
    // GET /api/AccommodationBookings/{id}
    // =========================================================

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var visitorIdRaw =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!Guid.TryParse(visitorIdRaw, out var visitorId))
        {
            return Unauthorized(new
            {
                message = "Visitor identity could not be verified."
            });
        }

        var booking =
            await _db.AccommodationBookings
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.VisitorId == visitorId);

        if (booking == null)
        {
            return NotFound(new
            {
                message = "Accommodation booking not found."
            });
        }

        return Ok(ToResponse(booking));
    }


    // =========================================================
    // GET VISITOR ACCOMMODATION BOOKINGS
    // GET /api/AccommodationBookings/my
    // =========================================================

    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings()
    {
        var visitorIdRaw =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!Guid.TryParse(visitorIdRaw, out var visitorId))
        {
            return Unauthorized(new
            {
                message = "Visitor identity could not be verified."
            });
        }

        var bookings =
            await _db.AccommodationBookings
                .AsNoTracking()
                .Where(a =>
                    a.VisitorId == visitorId)
                .OrderByDescending(a =>
                    a.CreatedAt)
                .ToListAsync();

        var response =
            bookings
                .Select(ToResponse)
                .ToList();

        return Ok(response);
    }


    // =========================================================
    // RESPONSE MAPPER
    // =========================================================

    private static AccommodationBookingResponse ToResponse(
        AccommodationBooking booking)
    {
        return new AccommodationBookingResponse
        {
            Id = booking.Id,

            AccommodationId =
                booking.AccommodationId,

            AccommodationName =
                booking.AccommodationName,

            CheckInDate =
                booking.CheckInDate,

            CheckOutDate =
                booking.CheckOutDate,

            NumberOfNights =
                booking.NumberOfNights,

            GuestCount =
                booking.GuestCount,

            PricePerNight =
                booking.PricePerNight,

            TotalPrice =
                booking.TotalPrice,

            Status =
                booking.Status,

            CreatedAt =
                booking.CreatedAt
        };
    }
}