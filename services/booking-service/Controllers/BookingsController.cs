using BookingService.Data;
using BookingService.DTOs;
using BookingService.Events;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Kafka;
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

    // POST: /api/bookings
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

        // 8. Check participant count against experience maximum
        if (request.ParticipantCount > listing.MaxParticipants)
        {
            return BadRequest(new
            {
                message =
                    $"Maximum participants allowed is {listing.MaxParticipants}."
            });
        }

        // 9. Retrieve availability from Provider Catalog
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

        // 10. Check whether experience operates on selected date
        if (!availability.IsOperatingDay)
        {
            return BadRequest(new
            {
                message =
                    "The experience is not available on the selected date."
            });
        }

        // 11. Check whether the whole day is fully booked
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

        // 13. Preliminary capacity validation
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

        // 14. Reserve the capacity in Provider Catalog.
        // This check happens immediately before booking creation.
        // If another visitor already reserved the remaining capacity,
        // this request will fail here instead of creating an overbooking.
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

        // 15. Get price from Provider Catalog
        var unitPrice = listing.Price;

        // 16. Calculate total amount
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

        // 19. Create booking.created Kafka event
        var bookingCreatedEvent =
            new BookingCreatedEvent
            {
                BookingId = booking.Id,
                VisitorId = booking.VisitorId,
                ListingId = booking.ListingId,
                BookingDate =
                    booking.BookingDate.ToString("yyyy-MM-dd"),
                TimeSlot = booking.TimeSlot,
                ParticipantCount =
                    booking.ParticipantCount,
                TotalAmount =
                    booking.TotalAmount
            };

        // 20. Publish booking.created event
        await _kafkaProducer.PublishAsync(
            "booking.created",
            booking.Id.ToString(),
            bookingCreatedEvent
        );

        // 21. Return created booking
        return CreatedAtAction(
            nameof(GetBookingById),
            new { id = booking.Id },
            booking
        );
    }

    // GET: /api/bookings/{id}
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

    // GET: /api/bookings/my
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
                    b.CreatedAt)
                .ToListAsync();

        return Ok(bookings);
    }
}