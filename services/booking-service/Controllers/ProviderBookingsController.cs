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
    private readonly IIdentityService _identityService;

    public ProviderBookingsController(
        BookingDbContext context,
        ICatalogService catalogService,
        IIdentityService identityService)
    {
        _context = context;
        _catalogService = catalogService;
        _identityService = identityService;
    }

    // GET: /api/provider-bookings/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookingsAndReservations()
    {
        // -------------------------------------------------
        // 1. Read the provider's JWT.
        // -------------------------------------------------

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

        // -------------------------------------------------
        // 2. Ask Provider Catalog for this provider's
        //    activity and restaurant listings.
        //
        //    Catalog Service performs the ownership check.
        // -------------------------------------------------

        var activityListings =
            await _catalogService
                .GetMyActivityListingsAsync(accessToken);

        var restaurantListings =
            await _catalogService
                .GetMyRestaurantListingsAsync(accessToken);

        // -------------------------------------------------
        // 3. Build provider-owned service ID collections.
        // -------------------------------------------------

        var activityIds =
            activityListings
                .Select(x => x.Id)
                .ToHashSet();

        var restaurantIds =
            restaurantListings
                .Select(x => x.Id)
                .ToHashSet();

        // -------------------------------------------------
        // 4. Retrieve only experience bookings belonging
        //    to this provider's activity listings.
        // -------------------------------------------------

        var experienceBookings =
            await _context.Bookings
                .AsNoTracking()
                .Where(b =>
                    activityIds.Contains(b.ListingId))
                .Select(b =>
                    new ProviderBookingResponse
                    {
                        Id = b.Id,

                        CustomerId =
                            b.VisitorId,

                        ServiceId =
                            b.ListingId,

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

        // -------------------------------------------------
        // 5. Retrieve only restaurant reservations
        //    belonging to this provider's restaurants.
        // -------------------------------------------------

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

                        // Restaurant reservations currently
                        // do not have a separate payment state.
                        PaymentStatus = null,

                        TotalAmount =
                            r.TotalPrice,

                        CreatedAt =
                            r.CreatedAt
                    })
                .ToListAsync();

        // -------------------------------------------------
        // 6. Combine experience bookings and restaurant
        //    reservations.
        // -------------------------------------------------

        var result =
            experienceBookings
                .Concat(restaurantReservations)
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.CreatedAt)
                .ToList();

        // -------------------------------------------------
        // 7. Get each unique customer's name and email
        //    from Identity Service.
        //
        //    We only request each customer once even if
        //    they have multiple bookings.
        // -------------------------------------------------

        var customerIds =
            result
                .Select(x => x.CustomerId)
                .Distinct()
                .ToList();

        var customerProfiles =
            new Dictionary<Guid, BookingCustomerResponse>();

        foreach (var customerId in customerIds)
        {
            try
            {
                var customer =
                    await _identityService
                        .GetBookingCustomerAsync(
                            customerId,
                            accessToken);

                if (customer != null)
                {
                    customerProfiles[customerId] =
                        customer;
                }
            }
            catch
            {
                // Keep the booking available even if
                // customer profile lookup temporarily fails.
                // Frontend can fall back to CustomerId.
            }
        }

        // -------------------------------------------------
        // 8. Add customer name and email to each result.
        // -------------------------------------------------

        foreach (var item in result)
        {
            if (customerProfiles.TryGetValue(
                    item.CustomerId,
                    out var customer))
            {
                item.CustomerName =
                    customer.FullName;

                item.CustomerEmail =
                    customer.Email;
            }
        }

        // -------------------------------------------------
        // 9. Return unified provider booking list.
        // -------------------------------------------------

        return Ok(result);
    }
}