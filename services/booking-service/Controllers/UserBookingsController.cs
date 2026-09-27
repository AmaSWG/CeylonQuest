using BookingService.Data;
using BookingService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookingService.Controllers;

[ApiController]
[Route("api/user-bookings")]
[Authorize]
public class UserBookingsController : ControllerBase
{
    private readonly BookingDbContext _context;

    public UserBookingsController(BookingDbContext context)
    {
        _context = context;
    }

    // GET: /api/user-bookings/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookingsAndReservations()
    {
        // 1. Get logged-in visitor ID from JWT
        var visitorIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(visitorIdValue) ||
            !Guid.TryParse(visitorIdValue, out var visitorId))
        {
            return Unauthorized(new
            {
                message = "Invalid visitor authentication."
            });
        }

        // =====================================================
        // 2. EXPERIENCE BOOKINGS
        // =====================================================

        var experienceBookings =
            await _context.Bookings
                .AsNoTracking()
                .Where(b => b.VisitorId == visitorId)
                .Select(b => new UserBookingResponse
                {
                    Id = b.Id,
                    ServiceId = b.ListingId,

                    BookingType = "Experience Booking",
                    ServiceName = b.ListingTitle,

                    Date = b.BookingDate,
                    Time = b.TimeSlot,

                    PeopleCount = b.ParticipantCount,

                    Status = b.Status.ToString(),
                    PaymentStatus = b.PaymentStatus.ToString(),

                    UnitPrice = b.UnitPrice,
                    TotalAmount = b.TotalAmount,

                    CheckInDate = null,
                    CheckOutDate = null,
                    NumberOfNights = null,

                    CancellationReason = b.CancellationReason,
                    CancelledAt = b.CancelledAt,
                    RefundPercentage = b.RefundPercentage,
                    RefundAmount = b.RefundAmount,
                    RefundedAt = b.RefundedAt,

                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

        // =====================================================
        // 3. RESTAURANT RESERVATIONS
        // =====================================================

        var restaurantReservations =
            await _context.RestaurantReservations
                .AsNoTracking()
                .Where(r => r.VisitorId == visitorId)
                .Select(r => new UserBookingResponse
                {
                    Id = r.Id,
                    ServiceId = r.RestaurantId,

                    BookingType = "Restaurant Reservation",
                    ServiceName = r.RestaurantName,

                    Date = r.ReservationDate,
                    Time = r.TimeSlot,

                    PeopleCount = r.PartySize,

                    Status = r.Status.ToString(),

                    // Restaurant currently has no separate
                    // payment status.
                    PaymentStatus = null,

                    UnitPrice = r.PricePerPerson,
                    TotalAmount = r.TotalPrice,

                    CheckInDate = null,
                    CheckOutDate = null,
                    NumberOfNights = null,

                    CancellationReason = r.CancellationReason,
                    CancelledAt = r.CancelledAt,
                    RefundPercentage = r.RefundPercentage,
                    RefundAmount = r.RefundAmount,
                    RefundedAt = r.RefundedAt,

                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

        // =====================================================
        // 4. ACCOMMODATION BOOKINGS
        // =====================================================

        var accommodationBookings =
            await _context.AccommodationBookings
                .AsNoTracking()
                .Where(a => a.VisitorId == visitorId)
                .Select(a => new UserBookingResponse
                {
                    Id = a.Id,
                    ServiceId = a.AccommodationId,

                    BookingType = "Accommodation Booking",
                    ServiceName = a.AccommodationName,

                    // Main date used for ordering/display
                    Date = a.CheckInDate,

                    Time = "Stay",

                    PeopleCount = a.GuestCount,

                    Status = a.Status.ToString(),

                    // Accommodation currently has no
                    // separate payment status.
                    PaymentStatus = null,

                    UnitPrice = a.PricePerNight,
                    TotalAmount = a.TotalPrice,

                    // Accommodation-specific information
                    CheckInDate = a.CheckInDate,
                    CheckOutDate = a.CheckOutDate,
                    NumberOfNights = a.NumberOfNights,

                    // Cancellation / refund information
                    CancellationReason = a.CancellationReason,
                    CancelledAt = a.CancelledAt,
                    RefundPercentage = a.RefundPercentage,
                    RefundAmount = a.RefundAmount,
                    RefundedAt = a.RefundedAt,

                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

        // =====================================================
        // 5. COMBINE ALL THREE TYPES
        // =====================================================

        var result =
            experienceBookings
                .Concat(restaurantReservations)
                .Concat(accommodationBookings)
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.CreatedAt)
                .ToList();

        return Ok(result);
    }
}