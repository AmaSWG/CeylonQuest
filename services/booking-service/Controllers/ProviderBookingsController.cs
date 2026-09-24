using BookingService.Data;
using BookingService.DTOs;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Controllers;

[ApiController]
[Route("api/provider-bookings")]
[Authorize(Roles = "Provider")]
public class ProviderBookingsController : ControllerBase
{
    private readonly BookingDbContext _context;
    private readonly ICatalogService _catalogService;

    public ProviderBookingsController(
        BookingDbContext context,
        ICatalogService catalogService)
    {
        _context = context;
        _catalogService = catalogService;
    }

    // GET: /api/provider-bookings/my
    [HttpGet("my")]
    public async Task<IActionResult>
        GetMyBookingsAndReservations()
    {
        // -----------------------------------------------
        // 1. Read the provider's JWT.
        // -----------------------------------------------

        var authorization =
            Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorization) ||
            !authorization.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new
            {
                message = "Provider authentication is required."
            });
        }

        var accessToken =
            authorization["Bearer ".Length..].Trim();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Unauthorized(new
            {
                message = "Invalid provider authentication."
            });
        }

        // -----------------------------------------------
        // 2. Ask Provider Catalog for THIS provider's
        //    activity and restaurant listings.
        //
        // Catalog Service performs the ownership check.
        // -----------------------------------------------

        var activityListings =
            await _catalogService
                .GetMyActivityListingsAsync(accessToken);

        var restaurantListings =
            await _catalogService
                .GetMyRestaurantListingsAsync(accessToken);

        // -----------------------------------------------
        // 3. Build provider-owned service ID collections.
        // -----------------------------------------------

        var activityIds =
            activityListings
                .Select(x => x.Id)
                .ToHashSet();

        var restaurantIds =
            restaurantListings
                .Select(x => x.Id)
                .ToHashSet();

        // -----------------------------------------------
        // 4. Retrieve only experience bookings belonging
        //    to this provider's activity listings.
        // -----------------------------------------------

        var experienceBookings =
            await _context.Bookings
                .AsNoTracking()
                .Where(b =>
                    activityIds.Contains(b.ListingId))
                .Select(b =>
                    new ProviderBookingResponse
                    {
                        Id = b.Id,

                        CustomerId = b.VisitorId,

                        ServiceId = b.ListingId,

                        BookingType =
                            "Experience Booking",

                        ServiceName =
                            b.ListingTitle,

                        Date =
                            b.BookingDate,

                        Time =
                            b.TimeSlot,

                        PeopleCount =
                            b.ParticipantCount,

                        Status =
                            b.Status.ToString(),

                        PaymentStatus =
                            b.PaymentStatus.ToString(),

                        TotalAmount =
                            b.TotalAmount,

                        CreatedAt =
                            b.CreatedAt
                    })
                .ToListAsync();

        // -----------------------------------------------
        // 5. Retrieve only reservations belonging to
        //    this provider's restaurant listings.
        // -----------------------------------------------

        var restaurantReservations =
            await _context.RestaurantReservations
                .AsNoTracking()
                .Where(r =>
                    restaurantIds.Contains(r.RestaurantId))
                .Select(r =>
                    new ProviderBookingResponse
                    {
                        Id = r.Id,

                        CustomerId =
                            r.VisitorId,

                        ServiceId =
                            r.RestaurantId,

                        BookingType =
                            "Restaurant Reservation",

                        ServiceName =
                            r.RestaurantName,

                        Date =
                            r.ReservationDate,

                        Time =
                            r.TimeSlot,

                        PeopleCount =
                            r.PartySize,

                        Status =
                            r.Status.ToString(),

                        // No separate payment state for
                        // restaurant reservations.
                        PaymentStatus = null,

                        TotalAmount =
                            r.TotalPrice,

                        CreatedAt =
                            r.CreatedAt
                    })
                .ToListAsync();

        // -----------------------------------------------
        // 6. Return one unified provider list.
        // -----------------------------------------------

        var result =
            experienceBookings
                .Concat(restaurantReservations)
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.CreatedAt)
                .ToList();

        return Ok(result);
    }
}