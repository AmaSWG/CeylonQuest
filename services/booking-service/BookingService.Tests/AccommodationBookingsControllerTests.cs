using BookingService.Controllers;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Models;
using BookingService.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Shared.Kafka;

using Moq;

using System.Security.Claims;

using Xunit;

namespace BookingService.Tests;

public class AccommodationBookingsControllerTests
{
    private readonly Mock<ICatalogService> _catalogServiceMock;
    private readonly Mock<IKafkaProducer> _kafkaProducerMock;

    private readonly Guid _visitorId;
    private readonly Guid _accommodationId;

    public AccommodationBookingsControllerTests()
    {
        _catalogServiceMock = new Mock<ICatalogService>();
        _kafkaProducerMock = new Mock<IKafkaProducer>();

        _visitorId = Guid.NewGuid();
        _accommodationId = Guid.NewGuid();
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private BookingDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<BookingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        return new BookingDbContext(options);
    }

    private AccommodationBookingsController CreateController(
        BookingDbContext context,
        Guid? visitorId = null)
    {
        var controller =
            new AccommodationBookingsController(
                context,
                _catalogServiceMock.Object,
                _kafkaProducerMock.Object);

        if (visitorId.HasValue)
        {
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    visitorId.Value.ToString())
            };

            var identity =
                new ClaimsIdentity(
                    claims,
                    "TestAuthentication");

            var principal =
                new ClaimsPrincipal(identity);

            controller.ControllerContext =
                new ControllerContext
                {
                    HttpContext =
                        new DefaultHttpContext
                        {
                            User = principal
                        }
                };
        }
        else
        {
            controller.ControllerContext =
                new ControllerContext
                {
                    HttpContext =
                        new DefaultHttpContext()
                };
        }

        return controller;
    }

    private CreateAccommodationBookingRequest CreateValidRequest()
    {
        var checkIn =
            DateOnly.FromDateTime(
                DateTime.Now.AddDays(2));

        return new CreateAccommodationBookingRequest
        {
            AccommodationId = _accommodationId,
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(2),
            GuestCount = 2
        };
    }

    private CatalogAccommodationResponse CreateValidAccommodation()
    {
        return new CatalogAccommodationResponse
        {
            Id = _accommodationId,
            ProviderId = Guid.NewGuid(),
            ProviderBusinessName = "Test Provider",
            RoomType = "Mountain View Deluxe Room",
            PropertyType = "Hotel",
            Location = "Ella",
            PricePerNight = 15000m,
            MaxGuests = 4,
            BedDetails = "2 King Beds",
            MinStayNights = 2,
            Amenities = "WiFi, Pool",
            BathroomDetails = "Private Bathroom",
            Description = "Test accommodation",
            IsActive = true
        };
    }

    private CatalogAvailabilityResponse CreateValidAvailability()
    {
        return new CatalogAvailabilityResponse
        {
            ListingId = _accommodationId,

            Date =
                DateOnly.FromDateTime(
                        DateTime.Now.AddDays(2))
                    .ToString("yyyy-MM-dd"),

            IsOperatingDay = true,
            IsFullyBooked = false,

            Slots = new List<CatalogAvailabilitySlot>
            {
                new CatalogAvailabilitySlot
                {
                    TimeSlot = "Stay (Min 2 Nights)",
                    TotalCapacity = 1,
                    RemainingCapacity = 1,
                    IsFullyBooked = false
                }
            }
        };
    }

    private void SetupSuccessfulCatalog()
    {
        _catalogServiceMock
            .Setup(x =>
                x.GetAccommodationAsync(
                    _accommodationId))
            .ReturnsAsync(
                CreateValidAccommodation());

        _catalogServiceMock
            .Setup(x =>
                x.GetAvailabilityAsync(
                    _accommodationId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(
                CreateValidAvailability());

        _catalogServiceMock
            .Setup(x =>
                x.ReserveCapacityAsync(
                    _accommodationId,
                    It.IsAny<DateOnly>(),
                    It.IsAny<string>(),
                    1))
            .ReturnsAsync(true);
    }

    // =========================================================
    // TEST 1
    // Invalid authentication
    // =========================================================

    [Fact]
    public async Task Create_InvalidAuthentication_ReturnsUnauthorized()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(context);

        var request =
            CreateValidRequest();

        var result =
            await controller.Create(request);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // =========================================================
    // TEST 2
    // Empty AccommodationId
    // =========================================================

    [Fact]
    public async Task Create_EmptyAccommodationId_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        request.AccommodationId =
            Guid.Empty;

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 3
    // Past check-in
    // =========================================================

    [Fact]
    public async Task Create_PastCheckIn_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        request.CheckInDate =
            DateOnly.FromDateTime(
                DateTime.Now.AddDays(-1));

        request.CheckOutDate =
            DateOnly.FromDateTime(
                DateTime.Now.AddDays(2));

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 4
    // Check-out equal to check-in
    // =========================================================

    [Fact]
    public async Task Create_CheckOutEqualsCheckIn_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        request.CheckOutDate =
            request.CheckInDate;

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 5
    // Check-out before check-in
    // =========================================================

    [Fact]
    public async Task Create_CheckOutBeforeCheckIn_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        request.CheckOutDate =
            request.CheckInDate.AddDays(-1);

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 6
    // Invalid guest count
    // =========================================================

    [Fact]
    public async Task Create_ZeroGuestCount_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        request.GuestCount = 0;

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 7
    // Accommodation does not exist
    // =========================================================

    [Fact]
    public async Task Create_AccommodationNotFound_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        _catalogServiceMock
            .Setup(x =>
                x.GetAccommodationAsync(
                    _accommodationId))
            .ReturnsAsync(
                (CatalogAccommodationResponse?)null);

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 8
    // Inactive accommodation
    // =========================================================

    [Fact]
    public async Task Create_InactiveAccommodation_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        var accommodation =
            CreateValidAccommodation();

        accommodation.IsActive = false;

        _catalogServiceMock
            .Setup(x =>
                x.GetAccommodationAsync(
                    _accommodationId))
            .ReturnsAsync(accommodation);

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 9
    // Guest count exceeds MaxGuests
    // =========================================================

    [Fact]
    public async Task Create_GuestCountExceedsMaximum_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        request.GuestCount = 5;

        var accommodation =
            CreateValidAccommodation();

        accommodation.MaxGuests = 4;

        _catalogServiceMock
            .Setup(x =>
                x.GetAccommodationAsync(
                    _accommodationId))
            .ReturnsAsync(accommodation);

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 10
    // Minimum stay not satisfied
    // =========================================================

    [Fact]
    public async Task Create_MinimumStayNotSatisfied_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        request.CheckOutDate =
            request.CheckInDate.AddDays(1);

        var accommodation =
            CreateValidAccommodation();

        accommodation.MinStayNights = 2;

        _catalogServiceMock
            .Setup(x =>
                x.GetAccommodationAsync(
                    _accommodationId))
            .ReturnsAsync(accommodation);

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 11
    // Availability missing
    // =========================================================

    [Fact]
    public async Task Create_AvailabilityNotFound_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        _catalogServiceMock
            .Setup(x =>
                x.GetAccommodationAsync(
                    _accommodationId))
            .ReturnsAsync(
                CreateValidAccommodation());

        _catalogServiceMock
            .Setup(x =>
                x.GetAvailabilityAsync(
                    _accommodationId,
                    request.CheckInDate))
            .ReturnsAsync(
                (CatalogAvailabilityResponse?)null);

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 12
    // Accommodation not available on selected date
    // =========================================================

    [Fact]
    public async Task Create_NotOperatingDay_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        var availability =
            CreateValidAvailability();

        availability.IsOperatingDay = false;

        _catalogServiceMock
            .Setup(x =>
                x.GetAccommodationAsync(
                    _accommodationId))
            .ReturnsAsync(
                CreateValidAccommodation());

        _catalogServiceMock
            .Setup(x =>
                x.GetAvailabilityAsync(
                    _accommodationId,
                    request.CheckInDate))
            .ReturnsAsync(availability);

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 13
    // Accommodation fully booked
    // =========================================================

    [Fact]
    public async Task Create_FullyBooked_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        var availability =
            CreateValidAvailability();

        availability.IsFullyBooked = true;

        _catalogServiceMock
            .Setup(x =>
                x.GetAccommodationAsync(
                    _accommodationId))
            .ReturnsAsync(
                CreateValidAccommodation());

        _catalogServiceMock
            .Setup(x =>
                x.GetAvailabilityAsync(
                    _accommodationId,
                    request.CheckInDate))
            .ReturnsAsync(availability);

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 14
    // Required accommodation slot missing
    // =========================================================

    [Fact]
    public async Task Create_AvailabilitySlotMissing_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        var availability =
            CreateValidAvailability();

        availability.Slots =
            new List<CatalogAvailabilitySlot>();

        _catalogServiceMock
            .Setup(x =>
                x.GetAccommodationAsync(
                    _accommodationId))
            .ReturnsAsync(
                CreateValidAccommodation());

        _catalogServiceMock
            .Setup(x =>
                x.GetAvailabilityAsync(
                    _accommodationId,
                    request.CheckInDate))
            .ReturnsAsync(availability);

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 15
    // No remaining accommodation unit
    // =========================================================

    [Fact]
    public async Task Create_NoRemainingCapacity_ReturnsBadRequest()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        var availability =
            CreateValidAvailability();

        availability.Slots[0].RemainingCapacity = 0;
        availability.Slots[0].IsFullyBooked = true;

        _catalogServiceMock
            .Setup(x =>
                x.GetAccommodationAsync(
                    _accommodationId))
            .ReturnsAsync(
                CreateValidAccommodation());

        _catalogServiceMock
            .Setup(x =>
                x.GetAvailabilityAsync(
                    _accommodationId,
                    request.CheckInDate))
            .ReturnsAsync(availability);

        var result =
            await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // =========================================================
    // TEST 16
    // Capacity changed before booking was saved
    // =========================================================

    [Fact]
    public async Task Create_CapacityReservationFails_ReturnsConflict()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        _catalogServiceMock
            .Setup(x =>
                x.GetAccommodationAsync(
                    _accommodationId))
            .ReturnsAsync(
                CreateValidAccommodation());

        _catalogServiceMock
            .Setup(x =>
                x.GetAvailabilityAsync(
                    _accommodationId,
                    request.CheckInDate))
            .ReturnsAsync(
                CreateValidAvailability());

        _catalogServiceMock
            .Setup(x =>
                x.ReserveCapacityAsync(
                    _accommodationId,
                    request.CheckInDate,
                    "Stay (Min 2 Nights)",
                    1))
            .ReturnsAsync(false);

        var result =
            await controller.Create(request);

        Assert.IsType<ConflictObjectResult>(result);

        Assert.Empty(
            context.AccommodationBookings);
    }

    // =========================================================
    // TEST 17
    // Successful booking
    // =========================================================

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        SetupSuccessfulCatalog();

        var result =
            await controller.Create(request);

        var createdResult =
            Assert.IsType<CreatedAtActionResult>(result);

        Assert.NotNull(createdResult.Value);
    }

    // =========================================================
    // TEST 18
    // Successful booking stored in database
    // =========================================================

    [Fact]
    public async Task Create_ValidRequest_SavesBookingToDatabase()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        SetupSuccessfulCatalog();

        await controller.Create(request);

        var booking =
            await context.AccommodationBookings
                .SingleAsync();

        Assert.Equal(
            _visitorId,
            booking.VisitorId);

        Assert.Equal(
            _accommodationId,
            booking.AccommodationId);

        Assert.Equal(
            "Mountain View Deluxe Room",
            booking.AccommodationName);

        Assert.Equal(
            request.CheckInDate,
            booking.CheckInDate);

        Assert.Equal(
            request.CheckOutDate,
            booking.CheckOutDate);

        Assert.Equal(
            2,
            booking.NumberOfNights);

        Assert.Equal(
            2,
            booking.GuestCount);

        Assert.Equal(
            15000m,
            booking.PricePerNight);

        Assert.Equal(
            30000m,
            booking.TotalPrice);

        Assert.Equal(
            AccommodationBookingStatus.Confirmed,
            booking.Status);
    }

    // =========================================================
    // TEST 19
    // Response contains correct calculated values
    // =========================================================

    [Fact]
    public async Task Create_ValidRequest_ReturnsCorrectResponse()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        SetupSuccessfulCatalog();

        var result =
            await controller.Create(request);

        var createdResult =
            Assert.IsType<CreatedAtActionResult>(result);

        var response =
            Assert.IsType<AccommodationBookingResponse>(
                createdResult.Value);

        Assert.Equal(
            _accommodationId,
            response.AccommodationId);

        Assert.Equal(
            "Mountain View Deluxe Room",
            response.AccommodationName);

        Assert.Equal(
            request.CheckInDate,
            response.CheckInDate);

        Assert.Equal(
            request.CheckOutDate,
            response.CheckOutDate);

        Assert.Equal(
            2,
            response.NumberOfNights);

        Assert.Equal(
            request.GuestCount,
            response.GuestCount);

        Assert.Equal(
            15000m,
            response.PricePerNight);

        Assert.Equal(
            30000m,
            response.TotalPrice);

        Assert.Equal(
            AccommodationBookingStatus.Confirmed,
            response.Status);
    }

    // =========================================================
    // TEST 20
    // Accommodation reserves ONE UNIT, not GuestCount
    // =========================================================

    [Fact]
    public async Task Create_ValidRequest_ReservesOneAccommodationUnit()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        request.GuestCount = 4;

        SetupSuccessfulCatalog();

        await controller.Create(request);

        _catalogServiceMock.Verify(
            x =>
                x.ReserveCapacityAsync(
                    _accommodationId,
                    request.CheckInDate,
                    "Stay (Min 2 Nights)",
                    1),
            Times.Once);
    }

    // =========================================================
    // TEST 21
    // Invalid booking must not reserve capacity
    // =========================================================

    [Fact]
    public async Task Create_InvalidGuestCount_DoesNotReserveCapacity()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var request =
            CreateValidRequest();

        request.GuestCount = 0;

        await controller.Create(request);

        _catalogServiceMock.Verify(
            x =>
                x.ReserveCapacityAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()),
            Times.Never);
    }

    // =========================================================
    // TEST 22
    // GetById invalid authentication
    // =========================================================

    [Fact]
    public async Task GetById_InvalidAuthentication_ReturnsUnauthorized()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(context);

        var result =
            await controller.GetById(
                Guid.NewGuid());

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // =========================================================
    // TEST 23
    // GetById booking not found
    // =========================================================

    [Fact]
    public async Task GetById_BookingNotFound_ReturnsNotFound()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var result =
            await controller.GetById(
                Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // =========================================================
    // TEST 24
    // Visitor can retrieve own booking
    // =========================================================

    [Fact]
    public async Task GetById_OwnBooking_ReturnsOk()
    {
        await using var context =
            CreateDbContext();

        var booking =
            CreateStoredBooking(
                _visitorId);

        context.AccommodationBookings.Add(
            booking);

        await context.SaveChangesAsync();

        var controller =
            CreateController(
                context,
                _visitorId);

        var result =
            await controller.GetById(
                booking.Id);

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var response =
            Assert.IsType<AccommodationBookingResponse>(
                okResult.Value);

        Assert.Equal(
            booking.Id,
            response.Id);

        Assert.Equal(
            booking.AccommodationId,
            response.AccommodationId);

        Assert.Equal(
            30000m,
            response.TotalPrice);
    }

    // =========================================================
    // TEST 25
    // Visitor cannot retrieve another visitor's booking
    // =========================================================

    [Fact]
    public async Task GetById_OtherVisitorsBooking_ReturnsNotFound()
    {
        await using var context =
            CreateDbContext();

        var otherVisitorId =
            Guid.NewGuid();

        var booking =
            CreateStoredBooking(
                otherVisitorId);

        context.AccommodationBookings.Add(
            booking);

        await context.SaveChangesAsync();

        var controller =
            CreateController(
                context,
                _visitorId);

        var result =
            await controller.GetById(
                booking.Id);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // =========================================================
    // TEST 26
    // GetMyBookings invalid authentication
    // =========================================================

    [Fact]
    public async Task GetMyBookings_InvalidAuthentication_ReturnsUnauthorized()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(context);

        var result =
            await controller.GetMyBookings();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // =========================================================
    // TEST 27
    // GetMyBookings returns only current visitor's bookings
    // =========================================================

    [Fact]
    public async Task GetMyBookings_ReturnsOnlyCurrentVisitorsBookings()
    {
        await using var context =
            CreateDbContext();

        var myBooking =
            CreateStoredBooking(
                _visitorId);

        var otherBooking =
            CreateStoredBooking(
                Guid.NewGuid());

        context.AccommodationBookings.AddRange(
            myBooking,
            otherBooking);

        await context.SaveChangesAsync();

        var controller =
            CreateController(
                context,
                _visitorId);

        var result =
            await controller.GetMyBookings();

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<
                IEnumerable<AccommodationBookingResponse>>(
                    okResult.Value);

        var list =
            bookings.ToList();

        Assert.Single(list);

        Assert.Equal(
            myBooking.Id,
            list[0].Id);
    }

    // =========================================================
    // TEST 28
    // GetMyBookings empty list
    // =========================================================

    [Fact]
    public async Task GetMyBookings_NoBookings_ReturnsEmptyList()
    {
        await using var context =
            CreateDbContext();

        var controller =
            CreateController(
                context,
                _visitorId);

        var result =
            await controller.GetMyBookings();

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<
                IEnumerable<AccommodationBookingResponse>>(
                    okResult.Value);

        Assert.Empty(bookings);
    }

    // =========================================================
    // STORED BOOKING HELPER
    // =========================================================

    private AccommodationBooking CreateStoredBooking(
        Guid visitorId)
    {
        var checkIn =
            DateOnly.FromDateTime(
                DateTime.Now.AddDays(2));

        return new AccommodationBooking
        {
            Id = Guid.NewGuid(),

            VisitorId = visitorId,

            AccommodationId =
                _accommodationId,

            AccommodationName =
                "Mountain View Deluxe Room",

            CheckInDate =
                checkIn,

            CheckOutDate =
                checkIn.AddDays(2),

            NumberOfNights = 2,

            GuestCount = 2,

            PricePerNight = 15000m,

            TotalPrice = 30000m,

            Status =
                AccommodationBookingStatus.Confirmed,

            CreatedAt =
                DateTime.UtcNow,

            UpdatedAt =
                DateTime.UtcNow
        };
    }
}