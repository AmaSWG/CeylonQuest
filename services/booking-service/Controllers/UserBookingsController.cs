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
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(visitorIdValue) ||
            !Guid.TryParse(visitorIdValue, out var visitorId))
        {
            return Unauthorized(new
            {
                message = "Invalid visitor authentication."
            });
        }

        // 2. Retrieve experience bookings
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
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

        // 3. Retrieve restaurant reservations
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

                    // Restaurant reservation currently has
                    // no separate payment status.
                    PaymentStatus = null,

                    UnitPrice = r.PricePerPerson,
                    TotalAmount = r.TotalPrice,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

        // 4. Combine both booking types
        var result =
            experienceBookings
                .Concat(restaurantReservations)
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.CreatedAt)
                .ToList();

        // 5. Return unified list
        return Ok(result);
    }
}