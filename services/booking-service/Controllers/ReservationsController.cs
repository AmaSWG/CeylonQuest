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
public class ReservationsController : ControllerBase
{
    private readonly BookingDbContext _context;
    private readonly ICatalogService _catalogService;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly TimeProvider _timeProvider;

    public ReservationsController(
        BookingDbContext context,
        ICatalogService catalogService,
        IKafkaProducer kafkaProducer,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _catalogService = catalogService;
        _kafkaProducer = kafkaProducer;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    // =========================================================
    // POST: /api/reservations
    // =========================================================
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
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
                _timeProvider.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo")).DateTime))
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

        var timeError = RestaurantReservationTime.Validate(
            request.ReservationDate, request.TimeSlot, _timeProvider.GetUtcNow());
        if (timeError != null)
            return BadRequest(new { message = timeError });

        // 5. Validate party size
        if (request.PartySize <= 0)
        {
            return BadRequest(new
            {
                message = "Party size must be at least 1."
            });
        }

        // 6. Get trusted restaurant details
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
        // A booking must carry the provider's identity ID for notification delivery.
        if (restaurant.ProviderUserId is null || restaurant.ProviderUserId == Guid.Empty)
        {
            return StatusCode(503, new { message = "The listing provider account is not linked. Please try again after it is configured." });
        }

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

        // 15. Calculate price on backend
        var pricePerPerson =
            restaurant.PricePerPerson;

        var totalPrice =
            pricePerPerson * request.PartySize;

        // 16. Reserve capacity
        timeError = RestaurantReservationTime.Validate(
            request.ReservationDate, request.TimeSlot, _timeProvider.GetUtcNow());
        if (timeError != null)
            return BadRequest(new { message = timeError });

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
        var reservation =
            new RestaurantReservation
            {
                ProviderId = restaurant.ProviderId == Guid.Empty ? null : restaurant.ProviderId,
                ScheduledEndAtUtc = ReviewSchedule.SlotEnd(request.ReservationDate, request.TimeSlot),
                Id = Guid.NewGuid(),

                VisitorId =
                    visitorId,

                RestaurantId =
                    request.RestaurantId,

                ProviderUserId =
                    restaurant.ProviderUserId,

                RestaurantName =
                    restaurant.Name,

                ReservationDate =
                    request.ReservationDate,

                TimeSlot =
                    request.TimeSlot,

                PartySize =
                    request.PartySize,

                PricePerPerson =
                    pricePerPerson,

                TotalPrice =
                    totalPrice,

                Status =
                    ReservationStatus.PendingPayment,

                CreatedAt =
                    DateTime.UtcNow,

                UpdatedAt =
                    DateTime.UtcNow
            };

        // 18. Save
        _context.RestaurantReservations.Add(
            reservation);

        await _context.SaveChangesAsync();

        await _kafkaProducer.PublishAsync(
            "booking.created",
            reservation.Id.ToString(),
            new BookingCreatedEvent
            {
                ProviderUserId = reservation.ProviderUserId,
                ProviderBusinessName = restaurant.ProviderBusinessName,
                BookingId = reservation.Id,
                VisitorId = reservation.VisitorId,
                ListingId = reservation.RestaurantId,
                ListingType = "Restaurant",
                BookingDate = reservation.ReservationDate.ToString("yyyy-MM-dd"),
                TimeSlot = reservation.TimeSlot,
                ParticipantCount = reservation.PartySize,
                TotalAmount = reservation.TotalPrice,
                CreatedAt = reservation.CreatedAt
            });

        // 19. Response
        var response =
            new RestaurantReservationResponse
            {
                Id =
                    reservation.Id,

                RestaurantId =
                    reservation.RestaurantId,

                RestaurantName =
                    reservation.RestaurantName,

                ReservationDate =
                    reservation.ReservationDate,

                TimeSlot =
                    reservation.TimeSlot,

                PartySize =
                    reservation.PartySize,

                PricePerPerson =
                    reservation.PricePerPerson,

                TotalPrice =
                    reservation.TotalPrice,

                Status =
                    reservation.Status,

                CreatedAt =
                    reservation.CreatedAt
            };

        return Ok(response);
    }

    // =========================================================
    // GET: /api/reservations/my
    // =========================================================
    [HttpGet("my")]
    public async Task<IActionResult> GetMyReservations()
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

        // 2. Retrieve reservations belonging to visitor
        var reservations =
            await _context.RestaurantReservations
                .AsNoTracking()
                .Where(r =>
                    r.VisitorId == visitorId &&
                    !r.IsDeleted)
                .OrderByDescending(r =>
                    r.ReservationDate)
                .ThenByDescending(r =>
                    r.CreatedAt)
                .Select(r =>
                    new RestaurantReservationListResponse
                    {
                        Id =
                            r.Id,

                        RestaurantId =
                            r.RestaurantId,

                        BookingType =
                            "Restaurant Reservation",

                        ServiceName =
                            r.RestaurantName,

                        Date =
                            r.ReservationDate,

                        Time =
                            r.TimeSlot,

                        PartySize =
                            r.PartySize,

                        Status =
                            r.Status.ToString(),

                        PricePerPerson =
                            r.PricePerPerson,

                        TotalAmount =
                            r.TotalPrice,

                        // Cancellation details
                        CancellationReason =
                            r.CancellationReason,

                        CancelledAt =
                            r.CancelledAt,

                        // Refund details
                        RefundPercentage =
                            r.RefundPercentage,

                        RefundAmount =
                            r.RefundAmount,

                        RefundedAt =
                            r.RefundedAt,

                        CreatedAt =
                            r.CreatedAt
                    })
                .ToListAsync();

        return Ok(reservations);
    }

    // =========================================================
    // DELETE: /api/reservations/{id}
    // =========================================================
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteReservation(Guid id)
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

        var reservation =
            await _context.RestaurantReservations
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.VisitorId == visitorId);

        if (reservation == null)
        {
            return NotFound(new
            {
                message = "Restaurant reservation not found."
            });
        }

        if (reservation.IsDeleted)
        {
            return NoContent();
        }

        reservation.IsDeleted = true;
        reservation.DeletedAt = DateTime.UtcNow;
        reservation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // =========================================================
    // PUT: /api/reservations/{id}/cancel
    // =========================================================
    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> CancelReservation(
        Guid id,
        [FromBody] CancelRestaurantReservationRequest request)
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

        // 2. Find reservation belonging to visitor
        var reservation =
            await _context.RestaurantReservations
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.VisitorId == visitorId &&
                    !r.IsDeleted);

        if (reservation == null)
        {
            return NotFound(new
            {
                message =
                    "Restaurant reservation not found."
            });
        }

        // 3. Prevent duplicate cancellation
        if (reservation.Status ==
            ReservationStatus.Cancelled)
        {
            return BadRequest(new
            {
                message =
                    "This restaurant reservation has already been cancelled."
            });
        }

        // 4. Validate time slot
        if (string.IsNullOrWhiteSpace(
            reservation.TimeSlot))
        {
            return BadRequest(new
            {
                message =
                    "The reservation time slot is invalid."
            });
        }

        // Example:
        // "08:00 AM - 10:00 AM"
        var timeParts =
            reservation.TimeSlot.Split(
                '-',
                StringSplitOptions.TrimEntries |
                StringSplitOptions.RemoveEmptyEntries);

        if (timeParts.Length < 1)
        {
            return BadRequest(new
            {
                message =
                    "The reservation time slot is invalid."
            });
        }

        var startTimeText =
            timeParts[0].Trim();

        // 5. Parse reservation start time
        if (!DateTime.TryParseExact(
                startTimeText,
                new[] { "hh:mm tt", "h:mm tt", "HH:mm", "H:mm" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedStartTime))
        {
            return BadRequest(new
            {
                message =
                    "The reservation start time could not be determined."
            });
        }

        // 6. Combine reservation date + start time
        var startTime =
            TimeOnly.FromDateTime(
                parsedStartTime);

        var reservationStartDateTime =
            reservation.ReservationDate
                .ToDateTime(startTime);

        // Current reservation date/time represents local time
        var currentDateTime =
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo"));

        // 7. Prevent cancellation of past reservation
        if (reservationStartDateTime <=
            currentDateTime)
        {
            return BadRequest(new
            {
                message =
                    "Past restaurant reservations cannot be cancelled."
            });
        }

        // 8. Calculate time remaining
        var timeUntilReservation =
            reservationStartDateTime -
            currentDateTime;

        var hoursUntilReservation =
            timeUntilReservation.TotalHours;

        // 9. Less than 24 hours = no cancellation

        // 10. Determine refund percentage
        decimal refundPercentage;

        if (hoursUntilReservation >= 48)
        {
            refundPercentage = 100m;
        }
        else
        {
            refundPercentage = hoursUntilReservation >= 24 ? 50m : 0m;
        }

        // 11. Calculate simulated refund
        decimal refundAmount = 0m;
        if (reservation.PaymentStatus == PaymentStatus.Paid)
        {
            refundAmount =
                reservation.TotalPrice *
                (refundPercentage / 100m);
        }

        // 12. Store cancellation information
        reservation.CancellationReason =
            request.Reason;

        reservation.CancelledAt =
            DateTime.UtcNow;

        reservation.RefundPercentage =
            refundPercentage;

        reservation.RefundAmount =
            refundAmount;

        if (reservation.PaymentStatus == PaymentStatus.Paid && refundAmount > 0)
        {
            reservation.PaymentStatus = PaymentStatus.Refunded;
            reservation.RefundedAt = DateTime.UtcNow;
        }
        else
        {
            reservation.RefundedAt = null;
        }

        // 13. Update reservation status
        reservation.Status =
            ReservationStatus.Cancelled;

        reservation.UpdatedAt =
            DateTime.UtcNow;

        // 14. Save cancellation
        await _context.SaveChangesAsync();

        // 15. Create capacity-restoration event
        var reservationCanceledEvent =
            new BookingCanceledEvent
            {
                ProviderUserId = reservation.ProviderUserId,
                VisitorId = reservation.VisitorId,
                BookingId =
                    reservation.Id,

                // For Restaurant, ListingId is RestaurantId
                ListingId =
                    reservation.RestaurantId,

                ListingType =
                    "Restaurant",

                BookingDate =
                    reservation.ReservationDate
                        .ToString("yyyy-MM-dd"),

                TimeSlot =
                    reservation.TimeSlot,

                // For Restaurant, ParticipantCount is PartySize
                ParticipantCount =
                    reservation.PartySize,

                Reason =
                    reservation.CancellationReason,

                CanceledAt =
                    reservation.CancelledAt ??
                    DateTime.UtcNow
            };

        // 16. Publish cancellation event
        await _kafkaProducer.PublishAsync(
            "booking.canceled",
            reservation.Id.ToString(),
            reservationCanceledEvent);

        // 17. Response
        return Ok(new
        {
            message =
                "Restaurant reservation cancelled successfully.",

            reservationId =
                reservation.Id,

            restaurantId =
                reservation.RestaurantId,

            status =
                reservation.Status.ToString(),

            totalAmount =
                reservation.TotalPrice,

            refundPercentage =
                reservation.RefundPercentage,

            refundAmount =
                reservation.RefundAmount,

            cancellationReason =
                reservation.CancellationReason,

            cancelledAt =
                reservation.CancelledAt,

            refundedAt =
                reservation.RefundedAt
        });
    }
}
