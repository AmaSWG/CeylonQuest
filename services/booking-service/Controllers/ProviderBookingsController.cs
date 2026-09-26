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

    // =========================================================
    // GET: /api/provider-bookings/my
    //
    // Returns:
    // - Experience bookings
    // - Restaurant reservations
    // - Accommodation bookings
    //
    // Only bookings belonging to the logged-in provider's
    // services are returned.
    // =========================================================

    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookingsAndReservations()
    {
        // -----------------------------------------------------
        // 1. Read provider JWT
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // 2. Get provider-owned listings from Catalog Service
        // -----------------------------------------------------

        var activityListings =
            await _catalogService
                .GetMyActivityListingsAsync(accessToken);

        var restaurantListings =
            await _catalogService
                .GetMyRestaurantListingsAsync(accessToken);

        var accommodationListings =
            await _catalogService
                .GetMyAccommodationListingsAsync(accessToken);

        // -----------------------------------------------------
        // 3. Get IDs owned by this provider
        // -----------------------------------------------------

        var activityIds =
            activityListings
                .Select(x => x.Id)
                .ToHashSet();

        var restaurantIds =
            restaurantListings
                .Select(x => x.Id)
                .ToHashSet();

        var accommodationIds =
            accommodationListings
                .Select(x => x.Id)
                .ToHashSet();

        // -----------------------------------------------------
        // 4. EXPERIENCE BOOKINGS
        // -----------------------------------------------------

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

                        BookingType = "Experience Booking",

                        ServiceName = b.ListingTitle,

                        Date = b.BookingDate,

                        Time = b.TimeSlot,

                        PeopleCount = b.ParticipantCount,

                        Status = b.Status.ToString(),

                        PaymentStatus =
                            b.PaymentStatus.ToString(),

                        UnitPrice = b.UnitPrice,

                        TotalAmount = b.TotalAmount,

                        CheckInDate = null,

                        CheckOutDate = null,

                        NumberOfNights = null,

                        CancellationReason =
                            b.CancellationReason,

                        CancelledAt =
                            b.CancelledAt,

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

        // -----------------------------------------------------
        // 5. RESTAURANT RESERVATIONS
        // -----------------------------------------------------

        var restaurantReservations =
            await _context.RestaurantReservations
                .AsNoTracking()
                .Where(r =>
                    restaurantIds.Contains(r.RestaurantId))
                .Select(r =>
                    new ProviderBookingResponse
                    {
                        Id = r.Id,

                        CustomerId = r.VisitorId,

                        ServiceId = r.RestaurantId,

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

                        PaymentStatus = null,

                        UnitPrice =
                            r.PricePerPerson,

                        TotalAmount =
                            r.TotalPrice,

                        CheckInDate = null,

                        CheckOutDate = null,

                        NumberOfNights = null,

                        CancellationReason =
                            r.CancellationReason,

                        CancelledAt =
                            r.CancelledAt,

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

        // -----------------------------------------------------
        // 6. ACCOMMODATION BOOKINGS
        // -----------------------------------------------------

        var accommodationBookings =
            await _context.AccommodationBookings
                .AsNoTracking()
                .Where(a =>
                    accommodationIds.Contains(
                        a.AccommodationId))
                .Select(a =>
                    new ProviderBookingResponse
                    {
                        Id = a.Id,

                        CustomerId = a.VisitorId,

                        ServiceId =
                            a.AccommodationId,

                        BookingType =
                            "Accommodation Booking",

                        ServiceName =
                            a.AccommodationName,

                        // Main date shown in provider table
                        Date =
                            a.CheckInDate,

                        Time =
                            "Stay",

                        PeopleCount =
                            a.GuestCount,

                        Status =
                            a.Status.ToString(),

                        PaymentStatus = null,

                        // Price per night
                        UnitPrice =
                            a.PricePerNight,

                        TotalAmount =
                            a.TotalPrice,

                        // Accommodation-specific fields
                        CheckInDate =
                            a.CheckInDate,

                        CheckOutDate =
                            a.CheckOutDate,

                        NumberOfNights =
                            a.NumberOfNights,

                        // Cancellation / refund fields
                        CancellationReason =
                            a.CancellationReason,

                        CancelledAt =
                            a.CancelledAt,

                        RefundPercentage =
                            a.RefundPercentage,

                        RefundAmount =
                            a.RefundAmount,

                        RefundedAt =
                            a.RefundedAt,

                        CreatedAt =
                            a.CreatedAt
                    })
                .ToListAsync();

        // -----------------------------------------------------
        // 7. Combine all three booking types
        // -----------------------------------------------------

        var result =
            experienceBookings
                .Concat(restaurantReservations)
                .Concat(accommodationBookings)
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.CreatedAt)
                .ToList();

        // -----------------------------------------------------
        // 8. Get unique customer profiles
        //
        // Uses your EXISTING IIdentityService.
        // This avoids calling Identity Service repeatedly for
        // the same customer.
        // -----------------------------------------------------

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
                // Keep booking information available even
                // if Identity Service temporarily fails.
            }
        }

        // -----------------------------------------------------
        // 9. Add customer name/email
        //
        // IMPORTANT:
        // BookingCustomerResponse uses FullName, NOT Name.
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // 10. Return provider booking list
        // -----------------------------------------------------

        return Ok(result);
    }
}