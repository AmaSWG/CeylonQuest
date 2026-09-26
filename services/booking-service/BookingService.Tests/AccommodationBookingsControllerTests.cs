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

    // TEST 29

    // Cancel >= 48 hours before check-in -> 100% refund

    // =========================================================



    [Fact]

    public async Task Cancel_MoreThan48HoursBeforeCheckIn_Returns100PercentRefund()

    {

        await using var context = CreateDbContext();



        var booking = CreateStoredBooking(_visitorId);



        booking.CheckInDate =

            DateOnly.FromDateTime(DateTime.Now.AddDays(5));



        booking.CheckOutDate =

            booking.CheckInDate.AddDays(2);



        booking.TotalPrice = 30000m;

        booking.Status = AccommodationBookingStatus.Confirmed;



        context.AccommodationBookings.Add(booking);

        await context.SaveChangesAsync();



        SetupCancellationCatalog();



        var controller =

            CreateController(context, _visitorId);



        var request =

            new CancelAccommodationBookingRequest

            {

                Reason = "Travel plans changed"

            };



        var result =

            await controller.Cancel(

                booking.Id,

                request);



        Assert.IsType<OkObjectResult>(result);



        var savedBooking =

            await context.AccommodationBookings

                .SingleAsync(x => x.Id == booking.Id);



        Assert.Equal(

            AccommodationBookingStatus.Cancelled,

            savedBooking.Status);



        Assert.Equal(

            100m,

            savedBooking.RefundPercentage);



        Assert.Equal(

            30000m,

            savedBooking.RefundAmount);



        Assert.Equal(

            "Travel plans changed",

            savedBooking.CancellationReason);



        Assert.NotNull(savedBooking.CancelledAt);

        Assert.NotNull(savedBooking.RefundedAt);

    }





    // =========================================================

    // TEST 30

    // Cancel 24-48 hours before check-in -> 50% refund

    // =========================================================



    [Fact]

    public async Task Cancel_Between24And48HoursBeforeCheckIn_Returns50PercentRefund()

    {

        await using var context = CreateDbContext();



        var booking = CreateStoredBooking(_visitorId);



        // DateOnly means the controller uses midnight as check-in time.

        // Tomorrow is normally between 24 and 48 hours away

        // when this test is run before midnight.

        booking.CheckInDate =

            DateOnly.FromDateTime(DateTime.Now.AddDays(2));



        booking.CheckOutDate =

            booking.CheckInDate.AddDays(2);



        booking.TotalPrice = 30000m;

        booking.Status = AccommodationBookingStatus.Confirmed;



        context.AccommodationBookings.Add(booking);

        await context.SaveChangesAsync();



        SetupCancellationCatalog();



        var controller =

            CreateController(context, _visitorId);



        var request =

            new CancelAccommodationBookingRequest

            {

                Reason = "Schedule changed"

            };



        var result =

            await controller.Cancel(

                booking.Id,

                request);



        var okResult =

            Assert.IsType<OkObjectResult>(result);



        Assert.NotNull(okResult.Value);



        var savedBooking =

            await context.AccommodationBookings

                .SingleAsync(x => x.Id == booking.Id);



        Assert.Equal(

            AccommodationBookingStatus.Cancelled,

            savedBooking.Status);



        Assert.Equal(

            50m,

            savedBooking.RefundPercentage);



        Assert.Equal(

            15000m,

            savedBooking.RefundAmount);



        Assert.Equal(

            "Schedule changed",

            savedBooking.CancellationReason);

    }





    // =========================================================

    // TEST 31

    // Cancel less than 24 hours before check-in -> rejected

    // =========================================================



    [Fact]

    public async Task Cancel_LessThan24HoursBeforeCheckIn_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var booking = CreateStoredBooking(_visitorId);



        booking.CheckInDate =

            DateOnly.FromDateTime(DateTime.Now.AddDays(1));



        booking.CheckOutDate =

            booking.CheckInDate.AddDays(2);



        booking.Status =

            AccommodationBookingStatus.Confirmed;



        context.AccommodationBookings.Add(booking);

        await context.SaveChangesAsync();



        var controller =

            CreateController(context, _visitorId);



        var request =

            new CancelAccommodationBookingRequest

            {

                Reason = "Late cancellation"

            };



        var result =

            await controller.Cancel(

                booking.Id,

                request);



        Assert.IsType<BadRequestObjectResult>(result);



        var savedBooking =

            await context.AccommodationBookings

                .SingleAsync(x => x.Id == booking.Id);



        Assert.Equal(

            AccommodationBookingStatus.Confirmed,

            savedBooking.Status);



        Assert.Equal(

            0m,

            savedBooking.RefundAmount);



        Assert.Null(savedBooking.CancelledAt);

    }





    // =========================================================

    // TEST 32

    // Already cancelled booking cannot be cancelled again

    // =========================================================



    [Fact]

    public async Task Cancel_AlreadyCancelledBooking_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var booking = CreateStoredBooking(_visitorId);



        booking.CheckInDate =

            DateOnly.FromDateTime(DateTime.Now.AddDays(5));



        booking.CheckOutDate =

            booking.CheckInDate.AddDays(2);



        booking.Status =

            AccommodationBookingStatus.Cancelled;



        context.AccommodationBookings.Add(booking);

        await context.SaveChangesAsync();



        var controller =

            CreateController(context, _visitorId);



        var request =

            new CancelAccommodationBookingRequest

            {

                Reason = "Cancel again"

            };



        var result =

            await controller.Cancel(

                booking.Id,

                request);



        Assert.IsType<BadRequestObjectResult>(result);



        _kafkaProducerMock.Verify(

            x => x.PublishAsync<BookingService.Events.BookingCanceledEvent>(

                It.IsAny<string>(),

                It.IsAny<string?>(),

                It.IsAny<BookingService.Events.BookingCanceledEvent>(),

                It.IsAny<CancellationToken>()),

            Times.Never);

    }





    // =========================================================

    // TEST 33

    // Completed booking cannot be cancelled

    // =========================================================



    [Fact]

    public async Task Cancel_CompletedBooking_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var booking = CreateStoredBooking(_visitorId);



        booking.CheckInDate =

            DateOnly.FromDateTime(DateTime.Now.AddDays(5));



        booking.CheckOutDate =

            booking.CheckInDate.AddDays(2);



        booking.Status =

            AccommodationBookingStatus.Completed;



        context.AccommodationBookings.Add(booking);

        await context.SaveChangesAsync();



        var controller =

            CreateController(context, _visitorId);



        var request =

            new CancelAccommodationBookingRequest

            {

                Reason = "Cancel completed booking"

            };



        var result =

            await controller.Cancel(

                booking.Id,

                request);



        Assert.IsType<BadRequestObjectResult>(result);



        _kafkaProducerMock.Verify(

            x => x.PublishAsync<BookingService.Events.BookingCanceledEvent>(

                It.IsAny<string>(),

                It.IsAny<string?>(),

                It.IsAny<BookingService.Events.BookingCanceledEvent>(),

                It.IsAny<CancellationToken>()),

            Times.Never);

    }





    // =========================================================

    // TEST 34

    // Other visitor cannot cancel booking

    // =========================================================



    [Fact]

    public async Task Cancel_OtherVisitorsBooking_ReturnsNotFound()

    {

        await using var context = CreateDbContext();



        var otherVisitorId =

            Guid.NewGuid();



        var booking =

            CreateStoredBooking(otherVisitorId);



        booking.CheckInDate =

            DateOnly.FromDateTime(DateTime.Now.AddDays(5));



        booking.CheckOutDate =

            booking.CheckInDate.AddDays(2);



        context.AccommodationBookings.Add(booking);

        await context.SaveChangesAsync();



        var controller =

            CreateController(

                context,

                _visitorId);



        var request =

            new CancelAccommodationBookingRequest

            {

                Reason = "Not my booking"

            };



        var result =

            await controller.Cancel(

                booking.Id,

                request);



        Assert.IsType<NotFoundObjectResult>(result);



        var savedBooking =

            await context.AccommodationBookings

                .SingleAsync(x => x.Id == booking.Id);



        Assert.Equal(

            AccommodationBookingStatus.Confirmed,

            savedBooking.Status);

    }





    // =========================================================

    // TEST 35

    // Non-existing booking -> NotFound

    // =========================================================



    [Fact]

    public async Task Cancel_BookingNotFound_ReturnsNotFound()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(

                context,

                _visitorId);



        var request =

            new CancelAccommodationBookingRequest

            {

                Reason = "Booking does not exist"

            };



        var result =

            await controller.Cancel(

                Guid.NewGuid(),

                request);



        Assert.IsType<NotFoundObjectResult>(result);

    }





    // =========================================================

    // TEST 36

    // Cancellation saves reason and refund information

    // =========================================================



    [Fact]

    public async Task Cancel_ValidBooking_SavesCancellationAndRefundDetails()

    {

        await using var context = CreateDbContext();



        var booking =

            CreateStoredBooking(_visitorId);



        booking.CheckInDate =

            DateOnly.FromDateTime(DateTime.Now.AddDays(5));



        booking.CheckOutDate =

            booking.CheckInDate.AddDays(3);



        booking.TotalPrice = 45000m;



        context.AccommodationBookings.Add(booking);

        await context.SaveChangesAsync();



        SetupCancellationCatalog();



        var controller =

            CreateController(

                context,

                _visitorId);



        var request =

            new CancelAccommodationBookingRequest

            {

                Reason = "Family emergency"

            };



        var result =

            await controller.Cancel(

                booking.Id,

                request);



        Assert.IsType<OkObjectResult>(result);



        var savedBooking =

            await context.AccommodationBookings

                .SingleAsync(x => x.Id == booking.Id);



        Assert.Equal(

            AccommodationBookingStatus.Cancelled,

            savedBooking.Status);



        Assert.Equal(

            "Family emergency",

            savedBooking.CancellationReason);



        Assert.Equal(

            100m,

            savedBooking.RefundPercentage);



        Assert.Equal(

            45000m,

            savedBooking.RefundAmount);



        Assert.NotNull(

            savedBooking.CancelledAt);



        Assert.NotNull(

            savedBooking.RefundedAt);

    }





    // =========================================================

    // TEST 37

    // Cancellation publishes booking.canceled Kafka event

    // =========================================================



    [Fact]

    public async Task Cancel_ValidBooking_PublishesBookingCanceledEvent()

    {

        await using var context = CreateDbContext();



        var booking =

            CreateStoredBooking(_visitorId);



        booking.CheckInDate =

            DateOnly.FromDateTime(DateTime.Now.AddDays(5));



        booking.CheckOutDate =

            booking.CheckInDate.AddDays(2);



        context.AccommodationBookings.Add(booking);

        await context.SaveChangesAsync();



        SetupCancellationCatalog();



        var controller =

            CreateController(

                context,

                _visitorId);



        var request =

            new CancelAccommodationBookingRequest

            {

                Reason = "Travel cancelled"

            };



        var result =

            await controller.Cancel(

                booking.Id,

                request);



        Assert.IsType<OkObjectResult>(result);



        _kafkaProducerMock.Verify(

            x => x.PublishAsync<BookingService.Events.BookingCanceledEvent>(

                "booking.canceled",

                It.IsAny<string?>(),

                It.Is<BookingService.Events.BookingCanceledEvent>(

                    e =>

                        e.BookingId == booking.Id &&

                        e.ListingId == booking.AccommodationId &&

                        e.ListingType == "Accommodation" &&

                        e.BookingDate ==

                            booking.CheckInDate.ToString("yyyy-MM-dd") &&

                        e.ParticipantCount == 1 &&

                        e.Reason == "Travel cancelled"),

                It.IsAny<CancellationToken>()),

            Times.Once);

    }





    // =========================================================

    // TEST 38

    // Accommodation cancellation releases ONE unit

    // =========================================================



    [Fact]

    public async Task Cancel_ValidBooking_KafkaEventReleasesOneAccommodationUnit()

    {

        await using var context = CreateDbContext();



        var booking =

            CreateStoredBooking(_visitorId);



        booking.GuestCount = 4;



        booking.CheckInDate =

            DateOnly.FromDateTime(DateTime.Now.AddDays(5));



        booking.CheckOutDate =

            booking.CheckInDate.AddDays(2);



        context.AccommodationBookings.Add(booking);

        await context.SaveChangesAsync();



        SetupCancellationCatalog();



        var controller =

            CreateController(

                context,

                _visitorId);



        var request =

            new CancelAccommodationBookingRequest

            {

                Reason = "Plans changed"

            };



        var result =

            await controller.Cancel(

                booking.Id,

                request);



        Assert.IsType<OkObjectResult>(result);



        _kafkaProducerMock.Verify(

            x => x.PublishAsync<BookingService.Events.BookingCanceledEvent>(

                "booking.canceled",

                It.IsAny<string?>(),

                It.Is<BookingService.Events.BookingCanceledEvent>(

                    e =>

                        e.BookingId == booking.Id &&

                        e.ParticipantCount == 1),

                It.IsAny<CancellationToken>()),

            Times.Once);

    }





    // =========================================================

    // TEST 39

    // GetById returns cancelled/refund information

    // =========================================================



    [Fact]

    public async Task GetById_CancelledBooking_ReturnsRefundDetails()

    {

        await using var context = CreateDbContext();



        var booking =

            CreateStoredBooking(_visitorId);



        booking.Status =

            AccommodationBookingStatus.Cancelled;



        booking.CancellationReason =

            "Travel plans changed";



        booking.CancelledAt =

            DateTime.UtcNow;



        booking.RefundPercentage =

            100m;



        booking.RefundAmount =

            30000m;



        booking.RefundedAt =

            DateTime.UtcNow;



        context.AccommodationBookings.Add(booking);

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

            AccommodationBookingStatus.Cancelled,

            response.Status);



        Assert.Equal(

            "Travel plans changed",

            response.CancellationReason);



        Assert.Equal(

            100m,

            response.RefundPercentage);



        Assert.Equal(

            30000m,

            response.RefundAmount);



        Assert.NotNull(

            response.CancelledAt);



        Assert.NotNull(

            response.RefundedAt);

    }





    // =========================================================

    // TEST 40

    // GetMyBookings returns cancelled/refund information

    // =========================================================



    [Fact]

    public async Task GetMyBookings_CancelledBooking_ReturnsRefundDetails()

    {

        await using var context = CreateDbContext();



        var booking =

            CreateStoredBooking(_visitorId);



        booking.Status =

            AccommodationBookingStatus.Cancelled;



        booking.CancellationReason =

            "Schedule changed";



        booking.CancelledAt =

            DateTime.UtcNow;



        booking.RefundPercentage =

            50m;



        booking.RefundAmount =

            15000m;



        booking.RefundedAt =

            DateTime.UtcNow;



        context.AccommodationBookings.Add(booking);

        await context.SaveChangesAsync();



        var controller =

            CreateController(

                context,

                _visitorId);



        var result =

            await controller.GetMyBookings();



        var okResult =

            Assert.IsType<OkObjectResult>(result);



        var responses =

            Assert.IsAssignableFrom<

                IEnumerable<AccommodationBookingResponse>>(

                    okResult.Value);



        var response =

            Assert.Single(responses);



        Assert.Equal(

            AccommodationBookingStatus.Cancelled,

            response.Status);



        Assert.Equal(

            "Schedule changed",

            response.CancellationReason);



        Assert.Equal(

            50m,

            response.RefundPercentage);



        Assert.Equal(

            15000m,

            response.RefundAmount);

    }





    // =========================================================

    // CANCELLATION CATALOG HELPER

    // =========================================================



    private void SetupCancellationCatalog()

    {

        _catalogServiceMock

            .Setup(x =>

                x.GetAccommodationAsync(

                    _accommodationId))

            .ReturnsAsync(

                CreateValidAccommodation());

    }




    // =========================================================
    // STORED BOOKING HELPER
    // =========================================================

    private AccommodationBooking CreateStoredBooking(Guid visitorId)
    {
        var checkIn = DateOnly.FromDateTime(DateTime.Now.AddDays(2));

        return new AccommodationBooking
        {
            Id = Guid.NewGuid(),
            VisitorId = visitorId,
            AccommodationId = _accommodationId,
            AccommodationName = "Mountain View Deluxe Room",
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(2),
            NumberOfNights = 2,
            GuestCount = 2,
            PricePerNight = 15000m,
            TotalPrice = 30000m,
            Status = AccommodationBookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

}
