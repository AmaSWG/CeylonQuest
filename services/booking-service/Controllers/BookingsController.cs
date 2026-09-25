using BookingService.Data;
using BookingService.DTOs;
using BookingService.Events;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Kafka;
using System.Globalization;
using System.Security.Claims;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly BookingDbContext _context;
    private readonly ICatalogService _catalogService;
    private readonly IKafkaProducer _kafkaProducer;

    public BookingsController(
        BookingDbContext context,
        ICatalogService catalogService,
        IKafkaProducer kafkaProducer)
    {
        _context = context;
        _catalogService = catalogService;
        _kafkaProducer = kafkaProducer;
    }

    // =========================================================
    // POST: /api/bookings
    // =========================================================
    [HttpPost]
    public async Task<IActionResult> CreateBooking(
        [FromBody] CreateBookingRequest request)
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

        // 2. Validate listing ID
        if (request.ListingId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A valid experience is required."
            });
        }

        // 3. Validate booking date
        if (request.BookingDate <
            DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new
            {
                message = "Booking date cannot be in the past."
            });
        }

        // 4. Validate time slot
        if (string.IsNullOrWhiteSpace(request.TimeSlot))
        {
            return BadRequest(new
            {
                message = "A time slot is required."
            });
        }

        // 5. Validate participant count
        if (request.ParticipantCount <= 0)
        {
            return BadRequest(new
            {
                message = "Participant count must be at least 1."
            });
        }

        // 6. Get experience details from Provider Catalog
        var listing =
            await _catalogService.GetListingAsync(
                request.ListingId);

        if (listing == null)
        {
            return BadRequest(new
            {
                message = "Experience could not be found."
            });
        }

        // 7. Check whether listing is active
        if (!listing.IsActive)
        {
            return BadRequest(new
            {
                message =
                    "This experience is currently unavailable."
            });
        }

        // 8. Check participant count against maximum
        if (request.ParticipantCount > listing.MaxParticipants)
        {
            return BadRequest(new
            {
                message =
                    $"Maximum participants allowed is {listing.MaxParticipants}."
            });
        }

        // 9. Retrieve availability
        var availability =
            await _catalogService.GetAvailabilityAsync(
                request.ListingId,
                request.BookingDate);

        if (availability == null)
        {
            return BadRequest(new
            {
                message =
                    "Could not retrieve experience availability."
            });
        }

        // 10. Check operating day
        if (!availability.IsOperatingDay)
        {
            return BadRequest(new
            {
                message =
                    "The experience is not available on the selected date."
            });
        }

        // 11. Check fully booked
        if (availability.IsFullyBooked)
        {
            return BadRequest(new
            {
                message =
                    "The experience is fully booked on the selected date."
            });
        }

        // 12. Find selected time slot
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
                    "The selected time slot is not available."
            });
        }

        // 13. Capacity validation
        if (selectedSlot.IsFullyBooked ||
            request.ParticipantCount >
            selectedSlot.RemainingCapacity)
        {
            return BadRequest(new
            {
                message =
                    $"Only {selectedSlot.RemainingCapacity} places are available."
            });
        }

        // 14. Reserve capacity
        var capacityReserved =
            await _catalogService.ReserveCapacityAsync(
                request.ListingId,
                request.BookingDate,
                request.TimeSlot,
                request.ParticipantCount);

        if (!capacityReserved)
        {
            return Conflict(new
            {
                message =
                    "The selected places are no longer available. Please check availability and try again."
            });
        }

        // 15. Get price
        var unitPrice = listing.Price;

        // 16. Calculate total
        var totalAmount =
            unitPrice * request.ParticipantCount;

        // 17. Create booking
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            VisitorId = visitorId,
            ListingId = request.ListingId,
            ListingTitle = listing.Title,
            ListingType = "Experience",
            BookingDate = request.BookingDate,
            TimeSlot = request.TimeSlot,
            ParticipantCount = request.ParticipantCount,
            UnitPrice = unitPrice,
            TotalAmount = totalAmount,
            Status = BookingStatus.PendingPayment,
            PaymentStatus = PaymentStatus.Unpaid,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 18. Save booking
        _context.Bookings.Add(booking);

        await _context.SaveChangesAsync();

        // 19. Create booking.created event
        var bookingCreatedEvent =
            new BookingCreatedEvent
            {
                BookingId = booking.Id,
                VisitorId = booking.VisitorId,
                ListingId = booking.ListingId,

                BookingDate =
                    booking.BookingDate.ToString("yyyy-MM-dd"),

                TimeSlot =
                    booking.TimeSlot,

                ParticipantCount =
                    booking.ParticipantCount,

                TotalAmount =
                    booking.TotalAmount
            };

        // 20. Publish booking.created
        await _kafkaProducer.PublishAsync(
            "booking.created",
            booking.Id.ToString(),
            bookingCreatedEvent
        );

        // 21. Return booking
        return CreatedAtAction(
            nameof(GetBookingById),
            new { id = booking.Id },
            booking
        );
    }

    // =========================================================
    // GET: /api/bookings/{id}
    // =========================================================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBookingById(Guid id)
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

        var booking =
            await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b =>
                    b.Id == id &&
                    b.VisitorId == visitorId);

        if (booking == null)
        {
            return NotFound(new
            {
                message = "Booking not found."
            });
        }

        return Ok(booking);
    }

    // =========================================================
    // GET: /api/bookings/my
    // =========================================================
    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings()
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

        var bookings =
            await _context.Bookings
                .AsNoTracking()
                .Where(b =>
                    b.VisitorId == visitorId)
                .OrderByDescending(b =>
                    b.BookingDate)
                .ThenByDescending(b =>
                    b.CreatedAt)
                .Select(b =>
                    new ExperienceBookingResponse
                    {
                        Id = b.Id,

                        ListingId =
                            b.ListingId,

                        BookingType =
                            "Experience Booking",

                        ServiceName =
                            b.ListingTitle,

                        Date =
                            b.BookingDate,

                        Time =
                            b.TimeSlot,

                        ParticipantCount =
                            b.ParticipantCount,

                        Status =
                            b.Status.ToString(),

                        PaymentStatus =
                            b.PaymentStatus.ToString(),

                        UnitPrice =
                            b.UnitPrice,

                        TotalAmount =
                            b.TotalAmount,

                        // Cancellation details
                        CancellationReason =
                            b.CancellationReason,

                        CancelledAt =
                            b.CancelledAt,

                        // Refund details
                        RefundPercentage =
                            b.RefundPercentage,

                        RefundAmount =
                            b.RefundAmount,

                        RefundedAt =
                            b.RefundedAt,

                        CreatedAt =
                            b.CreatedAt
                    })
                .ToListAsync();

        return Ok(bookings);
    }

    // =========================================================
    // PUT: /api/bookings/{id}/cancel
    // =========================================================
    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(
        Guid id,
        [FromBody] CancelBookingRequest request)
    {
        // 1. Get logged-in visitor ID
        var visitorIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(visitorIdValue) ||
            !Guid.TryParse(visitorIdValue, out var visitorId))
        {
            return Unauthorized(new
            {
                message =
                    "Invalid visitor authentication."
            });
        }

        // 2. Find visitor's booking
        var booking =
            await _context.Bookings
                .FirstOrDefaultAsync(b =>
                    b.Id == id &&
                    b.VisitorId == visitorId);

        if (booking == null)
        {
            return NotFound(new
            {
                message = "Booking not found."
            });
        }

        // 3. Prevent duplicate cancellation
        if (booking.Status ==
            BookingStatus.Cancelled)
        {
            return BadRequest(new
            {
                message =
                    "This booking has already been cancelled."
            });
        }

        // 4. Completed booking cannot be cancelled
        if (booking.Status ==
            BookingStatus.Completed)
        {
            return BadRequest(new
            {
                message =
                    "Completed bookings cannot be cancelled."
            });
        }

        // 5. Prevent duplicate refund
        if (booking.PaymentStatus ==
            PaymentStatus.Refunded)
        {
            return BadRequest(new
            {
                message =
                    "This booking has already been refunded."
            });
        }

        // 6. Validate time slot
        if (string.IsNullOrWhiteSpace(
            booking.TimeSlot))
        {
            return BadRequest(new
            {
                message =
                    "The booking time slot is invalid."
            });
        }

        // Example:
        // "08:00 AM - 11:00 AM"
        var timeParts =
            booking.TimeSlot.Split(
                '-',
                StringSplitOptions.TrimEntries |
                StringSplitOptions.RemoveEmptyEntries);

        if (timeParts.Length < 1)
        {
            return BadRequest(new
            {
                message =
                    "The booking time slot is invalid."
            });
        }

        // Start time = "08:00 AM"
        var startTimeText =
            timeParts[0].Trim();

        // 7. Parse booking start time
        if (!DateTime.TryParseExact(
                startTimeText,
                "hh:mm tt",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedStartTime))
        {
            return BadRequest(new
            {
                message =
                    "The booking start time could not be determined."
            });
        }

        // 8. Combine date + start time
        var startTime =
            TimeOnly.FromDateTime(
                parsedStartTime);

        var bookingStartDateTime =
            booking.BookingDate
                .ToDateTime(startTime);

        // Current booking date/time represents local experience time
        var currentDateTime =
            DateTime.Now;

        // 9. Reject past booking
        if (bookingStartDateTime <=
            currentDateTime)
        {
            return BadRequest(new
            {
                message =
                    "Past bookings cannot be cancelled."
            });
        }

        // 10. Calculate remaining time
        var timeUntilBooking =
            bookingStartDateTime -
            currentDateTime;

        var hoursUntilBooking =
            timeUntilBooking.TotalHours;

        // 11. Reject cancellation under 24 hours
        if (hoursUntilBooking < 24)
        {
            return BadRequest(new
            {
                message =
                    "Bookings cannot be cancelled less than 24 hours before the experience.",

                hoursUntilBooking =
                    Math.Round(
                        hoursUntilBooking,
                        2)
            });
        }

        // 12. Determine refund percentage
        decimal refundPercentage;

        if (hoursUntilBooking >= 48)
        {
            refundPercentage = 100m;
        }
        else
        {
            refundPercentage = 50m;
        }

        // 13. Calculate actual refund
        decimal refundAmount = 0m;

        if (booking.PaymentStatus ==
            PaymentStatus.Paid)
        {
            refundAmount =
                booking.TotalAmount *
                (refundPercentage / 100m);
        }

        // 14. Store cancellation details
        booking.CancellationReason =
            request.Reason;

        booking.CancelledAt =
            DateTime.UtcNow;

        booking.RefundPercentage =
            refundPercentage;

        booking.RefundAmount =
            refundAmount;

        // 15. Simulated refund handling
        if (booking.PaymentStatus ==
                PaymentStatus.Paid &&
            refundAmount > 0)
        {
            booking.PaymentStatus =
                PaymentStatus.Refunded;

            booking.RefundedAt =
                DateTime.UtcNow;
        }

        // 16. Update booking status
        booking.Status =
            BookingStatus.Cancelled;

        booking.UpdatedAt =
            DateTime.UtcNow;

        // 17. Save cancellation
        await _context.SaveChangesAsync();

        // 18. Create booking.canceled event
        var bookingCanceledEvent =
            new BookingCanceledEvent
            {
                BookingId =
                    booking.Id,

                ListingId =
                    booking.ListingId,

                ListingType =
                    booking.ListingType,

                BookingDate =
                    booking.BookingDate
                        .ToString("yyyy-MM-dd"),

                TimeSlot =
                    booking.TimeSlot,

                ParticipantCount =
                    booking.ParticipantCount,

                Reason =
                    booking.CancellationReason,

                CanceledAt =
                    booking.CancelledAt ??
                    DateTime.UtcNow
            };

        // 19. Publish booking.canceled
        await _kafkaProducer.PublishAsync(
            "booking.canceled",
            booking.Id.ToString(),
            bookingCanceledEvent
        );

        // 20. Return cancellation result
        return Ok(new
        {
            message =
                "Booking cancelled successfully.",

            bookingId =
                booking.Id,

            status =
                booking.Status.ToString(),

            paymentStatus =
                booking.PaymentStatus.ToString(),

            totalAmount =
                booking.TotalAmount,

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
}