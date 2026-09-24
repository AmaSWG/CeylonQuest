using BookingService.Controllers;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BookingService.Tests;

public class ProviderBookingsControllerTests
{
    // =====================================================
    // TEST 1
    // Provider receives experience bookings belonging
    // to their own activity listings
    // =====================================================

    [Fact]
    public async Task GetProviderBookings_ReturnsOnlyProvidersExperienceBookings()
    {
        var providerActivityId = Guid.NewGuid();
        var otherActivityId = Guid.NewGuid();

        await using var context = CreateContext();

        context.Bookings.Add(
            CreateBooking(
                providerActivityId,
                Guid.NewGuid(),
                "Provider Experience"));

        context.Bookings.Add(
            CreateBooking(
                otherActivityId,
                Guid.NewGuid(),
                "Other Provider Experience"));

        await context.SaveChangesAsync();

        var catalogService = CreateCatalogMock(
            new List<CatalogProviderListingResponse>
            {
                new()
                {
                    Id = providerActivityId,
                    Title = "Provider Experience"
                }
            },
            new List<CatalogProviderListingResponse>());

        var identityService =
            CreateIdentityMock();

        var controller =
            CreateController(
                context,
                catalogService.Object,
                identityService.Object);

        var result =
            await controller.GetMyBookingsAndReservations();

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<ProviderBookingResponse>>(
                okResult.Value);

        var list = bookings.ToList();

        Assert.Single(list);

        Assert.Equal(
            providerActivityId,
            list[0].ServiceId);

        Assert.Equal(
            "Provider Experience",
            list[0].ServiceName);

        Assert.Equal(
            "Experience Booking",
            list[0].BookingType);

        Assert.DoesNotContain(
            list,
            x => x.ServiceId == otherActivityId);
    }


    // =====================================================
    // TEST 2
    // Provider receives restaurant reservations belonging
    // to their own restaurant listings
    // =====================================================

    [Fact]
    public async Task GetProviderBookings_ReturnsOnlyProvidersRestaurantReservations()
    {
        var providerRestaurantId = Guid.NewGuid();
        var otherRestaurantId = Guid.NewGuid();

        await using var context = CreateContext();

        context.RestaurantReservations.Add(
            CreateReservation(
                providerRestaurantId,
                Guid.NewGuid(),
                "Provider Restaurant"));

        context.RestaurantReservations.Add(
            CreateReservation(
                otherRestaurantId,
                Guid.NewGuid(),
                "Other Provider Restaurant"));

        await context.SaveChangesAsync();

        var catalogService = CreateCatalogMock(
            new List<CatalogProviderListingResponse>(),
            new List<CatalogProviderListingResponse>
            {
                new()
                {
                    Id = providerRestaurantId,
                    Name = "Provider Restaurant"
                }
            });

        var identityService =
            CreateIdentityMock();

        var controller =
            CreateController(
                context,
                catalogService.Object,
                identityService.Object);

        var result =
            await controller.GetMyBookingsAndReservations();

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<ProviderBookingResponse>>(
                okResult.Value);

        var list = bookings.ToList();

        Assert.Single(list);

        Assert.Equal(
            providerRestaurantId,
            list[0].ServiceId);

        Assert.Equal(
            "Provider Restaurant",
            list[0].ServiceName);

        Assert.Equal(
            "Restaurant Reservation",
            list[0].BookingType);

        Assert.DoesNotContain(
            list,
            x => x.ServiceId == otherRestaurantId);
    }


    // =====================================================
    // TEST 3
    // Backend ownership restriction prevents a provider
    // from receiving another provider's records
    // =====================================================

    [Fact]
    public async Task GetProviderBookings_DoesNotReturnOtherProvidersBookings()
    {
        var providerActivityId = Guid.NewGuid();
        var providerRestaurantId = Guid.NewGuid();

        var otherActivityId = Guid.NewGuid();
        var otherRestaurantId = Guid.NewGuid();

        await using var context = CreateContext();

        context.Bookings.Add(
            CreateBooking(
                providerActivityId,
                Guid.NewGuid(),
                "My Activity"));

        context.Bookings.Add(
            CreateBooking(
                otherActivityId,
                Guid.NewGuid(),
                "Other Activity"));

        context.RestaurantReservations.Add(
            CreateReservation(
                providerRestaurantId,
                Guid.NewGuid(),
                "My Restaurant"));

        context.RestaurantReservations.Add(
            CreateReservation(
                otherRestaurantId,
                Guid.NewGuid(),
                "Other Restaurant"));

        await context.SaveChangesAsync();

        var catalogService = CreateCatalogMock(
            new List<CatalogProviderListingResponse>
            {
                new()
                {
                    Id = providerActivityId,
                    Title = "My Activity"
                }
            },
            new List<CatalogProviderListingResponse>
            {
                new()
                {
                    Id = providerRestaurantId,
                    Name = "My Restaurant"
                }
            });

        var identityService =
            CreateIdentityMock();

        var controller =
            CreateController(
                context,
                catalogService.Object,
                identityService.Object);

        var result =
            await controller.GetMyBookingsAndReservations();

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<ProviderBookingResponse>>(
                okResult.Value);

        var list = bookings.ToList();

        Assert.Equal(2, list.Count);

        Assert.DoesNotContain(
            list,
            x => x.ServiceId == otherActivityId);

        Assert.DoesNotContain(
            list,
            x => x.ServiceId == otherRestaurantId);

        Assert.Contains(
            list,
            x => x.ServiceId == providerActivityId);

        Assert.Contains(
            list,
            x => x.ServiceId == providerRestaurantId);
    }


    // =====================================================
    // TEST 4
    // Experience bookings and restaurant reservations
    // are returned together
    // =====================================================

    [Fact]
    public async Task GetProviderBookings_ReturnsUnifiedList()
    {
        var activityId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();

        await using var context = CreateContext();

        context.Bookings.Add(
            CreateBooking(
                activityId,
                Guid.NewGuid(),
                "Cultural Experience"));

        context.RestaurantReservations.Add(
            CreateReservation(
                restaurantId,
                Guid.NewGuid(),
                "Seafood Restaurant"));

        await context.SaveChangesAsync();

        var catalogService = CreateCatalogMock(
            new List<CatalogProviderListingResponse>
            {
                new()
                {
                    Id = activityId,
                    Title = "Cultural Experience"
                }
            },
            new List<CatalogProviderListingResponse>
            {
                new()
                {
                    Id = restaurantId,
                    Name = "Seafood Restaurant"
                }
            });

        var identityService =
            CreateIdentityMock();

        var controller =
            CreateController(
                context,
                catalogService.Object,
                identityService.Object);

        var result =
            await controller.GetMyBookingsAndReservations();

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<ProviderBookingResponse>>(
                okResult.Value);

        var list = bookings.ToList();

        Assert.Equal(2, list.Count);

        Assert.Contains(
            list,
            x => x.BookingType ==
                 "Experience Booking");

        Assert.Contains(
            list,
            x => x.BookingType ==
                 "Restaurant Reservation");
    }


    // =====================================================
    // TEST 5
    // Customer name and email are populated using
    // Identity Service
    // =====================================================

    [Fact]
    public async Task GetProviderBookings_ReturnsCustomerNameAndEmail()
    {
        var activityId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        await using var context = CreateContext();

        context.Bookings.Add(
            CreateBooking(
                activityId,
                customerId,
                "Cooking Experience"));

        await context.SaveChangesAsync();

        var catalogService = CreateCatalogMock(
            new List<CatalogProviderListingResponse>
            {
                new()
                {
                    Id = activityId,
                    Title = "Cooking Experience"
                }
            },
            new List<CatalogProviderListingResponse>());

        var identityService =
            new Mock<IIdentityService>();

        identityService
            .Setup(x =>
                x.GetBookingCustomerAsync(
                    customerId,
                    It.IsAny<string>()))
            .ReturnsAsync(
                new BookingCustomerResponse
                {
                    Id = customerId,
                    FirstName = "Chanumi",
                    LastName = "Yavindi",
                    Email = "chanumi@example.com"
                });

        var controller =
            CreateController(
                context,
                catalogService.Object,
                identityService.Object);

        var result =
            await controller.GetMyBookingsAndReservations();

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<ProviderBookingResponse>>(
                okResult.Value);

        var booking =
            Assert.Single(bookings);

        Assert.Equal(
            customerId,
            booking.CustomerId);

        Assert.Equal(
            "Chanumi Yavindi",
            booking.CustomerName);

        Assert.Equal(
            "chanumi@example.com",
            booking.CustomerEmail);

        identityService.Verify(
            x => x.GetBookingCustomerAsync(
                customerId,
                It.IsAny<string>()),
            Times.Once);
    }


    // =====================================================
    // TEST 6
    // Restaurant reservation does not return a misleading
    // payment status
    // =====================================================

    [Fact]
    public async Task GetProviderBookings_RestaurantPaymentStatusIsNull()
    {
        var restaurantId = Guid.NewGuid();

        await using var context = CreateContext();

        context.RestaurantReservations.Add(
            CreateReservation(
                restaurantId,
                Guid.NewGuid(),
                "Arcadia Restaurant"));

        await context.SaveChangesAsync();

        var catalogService = CreateCatalogMock(
            new List<CatalogProviderListingResponse>(),
            new List<CatalogProviderListingResponse>
            {
                new()
                {
                    Id = restaurantId,
                    Name = "Arcadia Restaurant"
                }
            });

        var identityService =
            CreateIdentityMock();

        var controller =
            CreateController(
                context,
                catalogService.Object,
                identityService.Object);

        var result =
            await controller.GetMyBookingsAndReservations();

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<ProviderBookingResponse>>(
                okResult.Value);

        var reservation =
            Assert.Single(bookings);

        Assert.Equal(
            "Restaurant Reservation",
            reservation.BookingType);

        Assert.Null(
            reservation.PaymentStatus);
    }


    // =====================================================
    // TEST 7
    // Provider with no customer bookings gets empty list
    // =====================================================

    [Fact]
    public async Task GetProviderBookings_ReturnsEmptyList_WhenProviderHasNoBookings()
    {
        await using var context = CreateContext();

        var catalogService = CreateCatalogMock(
            new List<CatalogProviderListingResponse>(),
            new List<CatalogProviderListingResponse>());

        var identityService =
            CreateIdentityMock();

        var controller =
            CreateController(
                context,
                catalogService.Object,
                identityService.Object);

        var result =
            await controller.GetMyBookingsAndReservations();

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<ProviderBookingResponse>>(
                okResult.Value);

        Assert.Empty(bookings);

        identityService.Verify(
            x => x.GetBookingCustomerAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>()),
            Times.Never);
    }


    // =====================================================
    // TEST 8
    // Missing Bearer token returns Unauthorized
    // =====================================================

    [Fact]
    public async Task GetProviderBookings_ReturnsUnauthorized_WhenBearerTokenMissing()
    {
        await using var context = CreateContext();

        var catalogService =
            new Mock<ICatalogService>();

        var identityService =
            new Mock<IIdentityService>();

        var controller =
            new ProviderBookingsController(
                context,
                catalogService.Object,
                identityService.Object);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext()
            };

        var result =
            await controller.GetMyBookingsAndReservations();

        Assert.IsType<UnauthorizedObjectResult>(
            result);

        catalogService.Verify(
            x => x.GetMyActivityListingsAsync(
                It.IsAny<string>()),
            Times.Never);

        catalogService.Verify(
            x => x.GetMyRestaurantListingsAsync(
                It.IsAny<string>()),
            Times.Never);
    }


    // =====================================================
    // HELPERS
    // =====================================================

    private static BookingDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<BookingDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

        return new BookingDbContext(options);
    }


    private static Mock<ICatalogService> CreateCatalogMock(
        List<CatalogProviderListingResponse> activities,
        List<CatalogProviderListingResponse> restaurants)
    {
        var mock =
            new Mock<ICatalogService>();

        mock
            .Setup(x =>
                x.GetMyActivityListingsAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(activities);

        mock
            .Setup(x =>
                x.GetMyRestaurantListingsAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(restaurants);

        return mock;
    }


    private static Mock<IIdentityService> CreateIdentityMock()
    {
        var mock =
            new Mock<IIdentityService>();

        mock
            .Setup(x =>
                x.GetBookingCustomerAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>()))
            .ReturnsAsync(
                (BookingCustomerResponse?)null);

        return mock;
    }


    private static ProviderBookingsController CreateController(
        BookingDbContext context,
        ICatalogService catalogService,
        IIdentityService identityService)
    {
        var controller =
            new ProviderBookingsController(
                context,
                catalogService,
                identityService);

        var httpContext =
            new DefaultHttpContext();

        httpContext.Request.Headers.Authorization =
            "Bearer test-provider-token";

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext = httpContext
            };

        return controller;
    }


    private static Booking CreateBooking(
        Guid listingId,
        Guid visitorId,
        string listingTitle)
    {
        return new Booking
        {
            Id = Guid.NewGuid(),

            VisitorId = visitorId,

            ListingId = listingId,

            ListingTitle = listingTitle,

            ListingType = "Experience",

            BookingDate =
                DateOnly.FromDateTime(
                    DateTime.UtcNow.AddDays(5)),

            TimeSlot =
                "08:00 AM - 11:00 AM",

            ParticipantCount = 2,

            UnitPrice = 6500m,

            TotalAmount = 13000m,

            CreatedAt =
                DateTime.UtcNow
        };
    }


    private static RestaurantReservation CreateReservation(
        Guid restaurantId,
        Guid visitorId,
        string restaurantName)
    {
        return new RestaurantReservation
        {
            Id = Guid.NewGuid(),

            VisitorId = visitorId,

            RestaurantId = restaurantId,

            RestaurantName = restaurantName,

            ReservationDate =
                DateOnly.FromDateTime(
                    DateTime.UtcNow.AddDays(3)),

            TimeSlot =
                "11:00 AM - 12:00 PM",

            PartySize = 3,

            PricePerPerson = 2000m,

            TotalPrice = 6000m,

            CreatedAt =
                DateTime.UtcNow
        };
    }
}