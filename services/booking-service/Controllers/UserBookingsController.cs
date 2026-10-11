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
                .Where(b =>
                    b.VisitorId == visitorId &&
                    !b.IsDeleted)
                .Select(b => new UserBookingResponse
                {
                    Id = b.Id,
                    AttendanceStatus = b.AttendanceStatus.ToString(),
                    AttendanceVerifiedAtUtc = b.AttendanceVerifiedAtUtc,
                    ScheduledEndAtUtc = b.ScheduledEndAtUtc,
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
                .Where(r =>
                    r.VisitorId == visitorId &&
                    !r.IsDeleted)
                .Select(r => new UserBookingResponse
                {
                    Id = r.Id,
                    AttendanceStatus = r.AttendanceStatus.ToString(),
                    AttendanceVerifiedAtUtc = r.AttendanceVerifiedAtUtc,
                    ScheduledEndAtUtc = r.ScheduledEndAtUtc,
                    ServiceId = r.RestaurantId,

                    BookingType = "Restaurant Reservation",
                    ServiceName = r.RestaurantName,

                    Date = r.ReservationDate,
                    Time = r.TimeSlot,

                    PeopleCount = r.PartySize,

                    Status = r.Status.ToString(),

                    PaymentStatus = r.PaymentStatus.ToString(),

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
                .Where(a =>
                    a.VisitorId == visitorId &&
                    !a.IsDeleted)
                .Select(a => new UserBookingResponse
                {
                    Id = a.Id,
                    AttendanceStatus = a.AttendanceStatus.ToString(),
                    AttendanceVerifiedAtUtc = a.AttendanceVerifiedAtUtc,
                    ScheduledEndAtUtc = a.ScheduledEndAtUtc,
                    ServiceId = a.AccommodationId,

                    BookingType = "Accommodation Booking",
                    ServiceName = a.AccommodationName,

                    // Main date used for ordering/display
                    Date = a.CheckInDate,

                    Time = "Stay",

                    PeopleCount = a.GuestCount,

                    Status = a.Status.ToString(),

                    PaymentStatus = a.PaymentStatus.ToString(),

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

        var experienceEntities = await _context.Bookings.AsNoTracking().Where(b => b.VisitorId == visitorId && !b.IsDeleted).ToListAsync();
        var restaurantEntities = await _context.RestaurantReservations.AsNoTracking().Where(b => b.VisitorId == visitorId && !b.IsDeleted).ToListAsync();
        var accommodationEntities = await _context.AccommodationBookings.AsNoTracking().Where(b => b.VisitorId == visitorId && !b.IsDeleted).ToListAsync();
        var reviewRows = await _context.ListingReviews.AsNoTracking()
            .Where(r => r.VisitorId == visitorId).ToListAsync();
        foreach (var item in result)
        {
            var type = item.BookingType.StartsWith("Experience") ? "Experience"
                : item.BookingType.StartsWith("Restaurant") ? "Restaurant" : "Accommodation";
            var booking = type switch
            {
                "Experience" => (BookingService.Models.IPayableBooking?)experienceEntities.FirstOrDefault(b => b.Id == item.Id),
                "Restaurant" => restaurantEntities.FirstOrDefault(b => b.Id == item.Id),
                _ => accommodationEntities.FirstOrDefault(b => b.Id == item.Id)
            };
            var existing = reviewRows.FirstOrDefault(r => r.BookingType == type && r.BookingId == item.Id);
            item.ReviewId = existing?.Id;
            if (booking != null)
            {
                item.ScheduledEndAtUtc = BookingService.Services.ReviewService.ScheduledEnd(booking);
                item.ReviewIneligibilityReason = BookingService.Services.ReviewService.IneligibilityReason(booking, DateTime.UtcNow, existing != null);
                item.CanReview = item.ReviewIneligibilityReason == null;
            }
        }
        return Ok(result);
    }
}
