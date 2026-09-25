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

    public ReservationsController(
        BookingDbContext context,
        ICatalogService catalogService,
        IKafkaProducer kafkaProducer)
    {
        _context = context;
        _catalogService = catalogService;
        _kafkaProducer = kafkaProducer;
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
                Id = Guid.NewGuid(),

                VisitorId =
                    visitorId,

                RestaurantId =
                    request.RestaurantId,

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
                    ReservationStatus.Confirmed,

                CreatedAt =
                    DateTime.UtcNow,

                UpdatedAt =
                    DateTime.UtcNow
            };

        // 18. Save
        _context.RestaurantReservations.Add(
            reservation);

        await _context.SaveChangesAsync();

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
                    r.VisitorId == visitorId)
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
                    r.VisitorId == visitorId);

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
                "hh:mm tt",
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
            DateTime.Now;

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
        if (hoursUntilReservation < 24)
        {
            return BadRequest(new
            {
                message =
                    "Restaurant reservations cannot be cancelled less than 24 hours before the reservation.",

                hoursUntilReservation =
                    Math.Round(
                        hoursUntilReservation,
                        2)
            });
        }

        // 10. Determine refund percentage
        decimal refundPercentage;

        if (hoursUntilReservation >= 48)
        {
            refundPercentage = 100m;
        }
        else
        {
            refundPercentage = 50m;
        }

        // 11. Calculate simulated refund
        var refundAmount =
            reservation.TotalPrice *
            (refundPercentage / 100m);

        // 12. Store cancellation information
        reservation.CancellationReason =
            request.Reason;

        reservation.CancelledAt =
            DateTime.UtcNow;

        reservation.RefundPercentage =
            refundPercentage;

        reservation.RefundAmount =
            refundAmount;

        // Restaurant currently has no PaymentStatus.
        // For this story the refund is simulated.
        reservation.RefundedAt =
            DateTime.UtcNow;

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