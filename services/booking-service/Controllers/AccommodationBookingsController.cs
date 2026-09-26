using System.Security.Claims;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Events;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Kafka;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccommodationBookingsController : ControllerBase
{
    private readonly BookingDbContext _db;
    private readonly ICatalogService _catalogService;
    private readonly IKafkaProducer _kafkaProducer;

    public AccommodationBookingsController(
        BookingDbContext db,
        ICatalogService catalogService,
        IKafkaProducer kafkaProducer)
    {
        _db = db;
        _catalogService = catalogService;
        _kafkaProducer = kafkaProducer;
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

        // 1. Get visitor ID from JWT
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

        // 2. Basic validation
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

        // 3. Get accommodation
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

        // 4. Validate max guests
        if (request.GuestCount > accommodation.MaxGuests)
        {
            return BadRequest(new
            {
                message =
                    $"This accommodation allows a maximum of " +
                    $"{accommodation.MaxGuests} guest(s)."
            });
        }

        // 5. Calculate nights
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

        // 6. Build exact availability slot
        var slotName =
            $"Stay (Min {accommodation.MinStayNights} " +
            $"Night{(accommodation.MinStayNights > 1 ? "s" : "")})";

        // 7. Check availability
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

        // 8. Calculate trusted price
        var pricePerNight = accommodation.PricePerNight;

        var totalPrice =
            pricePerNight * numberOfNights;

        // 9. Reserve ONE accommodation unit
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

        // 10. Create booking
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

            CancellationReason = null,

            CancelledAt = null,

            RefundPercentage = 0m,

            RefundAmount = 0m,

            RefundedAt = null,

            CreatedAt =
                DateTime.UtcNow,

            UpdatedAt =
                DateTime.UtcNow
        };

        _db.AccommodationBookings.Add(booking);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = booking.Id },
            ToResponse(booking));
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
    // CANCEL ACCOMMODATION BOOKING
    // PUT /api/AccommodationBookings/{id}/cancel
    // =========================================================

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelAccommodationBookingRequest request)
    {
        // -----------------------------------------------------
        // 1. Validate visitor identity
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
        // 2. Find booking belonging to current visitor
        // -----------------------------------------------------

        var booking =
            await _db.AccommodationBookings
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

        // -----------------------------------------------------
        // 3. Prevent duplicate cancellation
        // -----------------------------------------------------

        if (booking.Status ==
            AccommodationBookingStatus.Cancelled)
        {
            return BadRequest(new
            {
                message =
                    "This accommodation booking has already been cancelled."
            });
        }

        // -----------------------------------------------------
        // 4. Completed booking cannot be cancelled
        // -----------------------------------------------------

        if (booking.Status ==
            AccommodationBookingStatus.Completed)
        {
            return BadRequest(new
            {
                message =
                    "Completed accommodation bookings cannot be cancelled."
            });
        }

        // -----------------------------------------------------
        // 5. Calculate time until check-in
        //
        // Accommodation currently stores a check-in DATE rather
        // than a separate check-in time.
        // Therefore the cancellation window is calculated from
        // the beginning of the check-in date.
        // -----------------------------------------------------

        var checkInDateTime =
            booking.CheckInDate.ToDateTime(
                TimeOnly.MinValue);

        var now = DateTime.Now;

        var hoursUntilCheckIn =
            (checkInDateTime - now).TotalHours;

        // -----------------------------------------------------
        // 6. Past bookings cannot be cancelled
        // -----------------------------------------------------

        if (hoursUntilCheckIn <= 0)
        {
            return BadRequest(new
            {
                message =
                    "Past accommodation bookings cannot be cancelled."
            });
        }

        // -----------------------------------------------------
        // 7. Less than 24 hours = not eligible
        // -----------------------------------------------------

        if (hoursUntilCheckIn < 24)
        {
            return BadRequest(new
            {
                message =
                    "Accommodation bookings cannot be cancelled " +
                    "less than 24 hours before check-in."
            });
        }

        // -----------------------------------------------------
        // 8. Calculate refund
        //
        // 48+ hours = 100%
        // 24-48 hours = 50%
        // -----------------------------------------------------

        decimal refundPercentage;

        if (hoursUntilCheckIn >= 48)
        {
            refundPercentage = 100m;
        }
        else
        {
            refundPercentage = 50m;
        }

        var refundAmount =
            Math.Round(
                booking.TotalPrice *
                (refundPercentage / 100m),
                2);

        // -----------------------------------------------------
        // 9. Get accommodation so we can reconstruct the exact
        //    availability slot used during booking creation.
        // -----------------------------------------------------

        var accommodation =
            await _catalogService.GetAccommodationAsync(
                booking.AccommodationId);

        if (accommodation == null)
        {
            return BadRequest(new
            {
                message =
                    "Accommodation information could not be found. " +
                    "Cancellation could not be completed."
            });
        }

        var slotName =
            $"Stay (Min {accommodation.MinStayNights} " +
            $"Night{(accommodation.MinStayNights > 1 ? "s" : "")})";

        // -----------------------------------------------------
        // 10. Update cancellation/refund information
        // -----------------------------------------------------

        var cancelledAt =
            DateTime.UtcNow;

        booking.Status =
            AccommodationBookingStatus.Cancelled;

        booking.CancellationReason =
            string.IsNullOrWhiteSpace(request.Reason)
                ? null
                : request.Reason.Trim();

        booking.CancelledAt =
            cancelledAt;

        booking.RefundPercentage =
            refundPercentage;

        booking.RefundAmount =
            refundAmount;

        // Simulated refund
        booking.RefundedAt =
            refundAmount > 0
                ? cancelledAt
                : null;

        booking.UpdatedAt =
            cancelledAt;

        // -----------------------------------------------------
        // 11. Save cancellation
        // -----------------------------------------------------

        await _db.SaveChangesAsync();

        // -----------------------------------------------------
        // 12. Publish booking.canceled event
        //
        // IMPORTANT:
        // Accommodation capacity represents one room/unit.
        // Therefore ParticipantCount = 1, NOT GuestCount.
        //
        // Existing provider-catalog consumer will restore the
        // capacity for this listing/date/slot.
        // -----------------------------------------------------

        var canceledEvent =
            new BookingCanceledEvent
            {
                BookingId =
                    booking.Id,

                ListingId =
                    booking.AccommodationId,

                ListingType =
                    "Accommodation",

                BookingDate =
                    booking.CheckInDate
                        .ToString("yyyy-MM-dd"),

                TimeSlot =
                    slotName,

                ParticipantCount =
                    1,

                Reason =
                    booking.CancellationReason,

                CanceledAt =
                    cancelledAt
            };

        await _kafkaProducer.PublishAsync(
            "booking.canceled",
            booking.Id.ToString(),
            canceledEvent);

        // -----------------------------------------------------
        // 13. Return result
        // -----------------------------------------------------

        return Ok(new
        {
            message =
                "Accommodation booking cancelled successfully.",

            bookingId =
                booking.Id,

            accommodationId =
                booking.AccommodationId,

            accommodationName =
                booking.AccommodationName,

            checkInDate =
                booking.CheckInDate,

            checkOutDate =
                booking.CheckOutDate,

            numberOfNights =
                booking.NumberOfNights,

            guestCount =
                booking.GuestCount,

            status =
                booking.Status,

            totalAmount =
                booking.TotalPrice,

            refundPercentage =
                booking.RefundPercentage,

            refundAmount =
                booking.RefundAmount,

            cancellationReason =
                booking.CancellationReason,

            cancelledAt =
                booking.CancelledAt,

            refundedAt =
                booking.RefundedAt
        });
    }

    // =========================================================
    // RESPONSE MAPPER
    // =========================================================

    private static AccommodationBookingResponse ToResponse(
        AccommodationBooking booking)
    {
        return new AccommodationBookingResponse
        {
            Id =
                booking.Id,

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

            CancellationReason =
                booking.CancellationReason,

            CancelledAt =
                booking.CancelledAt,

            RefundPercentage =
                booking.RefundPercentage,

            RefundAmount =
                booking.RefundAmount,

            RefundedAt =
                booking.RefundedAt,

            CreatedAt =
                booking.CreatedAt
        };
    }
}