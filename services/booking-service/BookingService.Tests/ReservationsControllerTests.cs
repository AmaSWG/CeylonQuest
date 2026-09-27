using BookingService.Controllers;

using BookingService.Data;

using BookingService.DTOs;

using BookingService.Models;

using BookingService.Services;



using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;



using Moq;

using Shared.Kafka;



using System.Security.Claims;



using Xunit;



namespace BookingService.Tests;



public class ReservationsControllerTests

{

    private readonly Mock<ICatalogService> _catalogServiceMock;

    private readonly Mock<IKafkaProducer> _kafkaProducerMock;

    private readonly Guid _visitorId;

    private readonly Guid _restaurantId;



    public ReservationsControllerTests()

    {

        _catalogServiceMock = new Mock<ICatalogService>();

        _kafkaProducerMock = new Mock<IKafkaProducer>();



        _visitorId = Guid.NewGuid();

        _restaurantId = Guid.NewGuid();

    }



    // ---------------------------------------------------------

    // Helper Methods

    // ---------------------------------------------------------



    private BookingDbContext CreateDbContext()

    {

        var options = new DbContextOptionsBuilder<BookingDbContext>()

            .UseInMemoryDatabase(Guid.NewGuid().ToString())

            .Options;



        return new BookingDbContext(options);

    }



    private ReservationsController CreateController(

        BookingDbContext context,

        Guid? visitorId = null)

    {

        var controller = new ReservationsController(

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



            var identity = new ClaimsIdentity(

                claims,

                "TestAuthentication");



            var principal = new ClaimsPrincipal(identity);



            controller.ControllerContext = new ControllerContext

            {

                HttpContext = new DefaultHttpContext

                {

                    User = principal

                }

            };

        }

        else

        {

            controller.ControllerContext = new ControllerContext

            {

                HttpContext = new DefaultHttpContext()

            };

        }



        return controller;

    }



    private CreateRestaurantReservationRequest CreateValidRequest()

    {

        return new CreateRestaurantReservationRequest

        {

            RestaurantId = _restaurantId,

            ReservationDate =

                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),

            TimeSlot = "09:00 AM - 10:00 AM",

            PartySize = 4

        };

    }



    private CatalogRestaurantResponse CreateValidRestaurant()

    {

        return new CatalogRestaurantResponse

        {

            Id = _restaurantId,

            Name = "Test Restaurant",

            PricePerPerson = 2000m,

            SeatingCapacity = 30,

            IsActive = true

        };

    }



    private CatalogAvailabilityResponse CreateValidAvailability()

    {

        return new CatalogAvailabilityResponse

        {

            ListingId = _restaurantId,

            Date =

                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))

                    .ToString("yyyy-MM-dd"),



            IsOperatingDay = true,

            IsFullyBooked = false,



            Slots = new List<CatalogAvailabilitySlot>

            {

                new CatalogAvailabilitySlot

                {

                    TimeSlot = "09:00 AM - 10:00 AM",

                    TotalCapacity = 30,

                    RemainingCapacity = 30,

                    IsFullyBooked = false

                }

            }

        };

    }



    private void SetupSuccessfulCatalog()

    {

        _catalogServiceMock

            .Setup(x => x.GetRestaurantAsync(_restaurantId))

            .ReturnsAsync(CreateValidRestaurant());



        _catalogServiceMock

            .Setup(x => x.GetAvailabilityAsync(

                _restaurantId,

                It.IsAny<DateOnly>()))

            .ReturnsAsync(CreateValidAvailability());



        _catalogServiceMock

            .Setup(x => x.ReserveCapacityAsync(

                _restaurantId,

                It.IsAny<DateOnly>(),

                It.IsAny<string>(),

                It.IsAny<int>()))

            .ReturnsAsync(true);

    }



    // =========================================================

    // TEST 1

    // Invalid / missing authentication

    // =========================================================



    [Fact]

    public async Task CreateReservation_InvalidAuthentication_ReturnsUnauthorized()

    {

        await using var context = CreateDbContext();



        var controller = CreateController(context);



        var request = CreateValidRequest();



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<UnauthorizedObjectResult>(result);

    }



    // =========================================================

    // TEST 2

    // Empty RestaurantId

    // =========================================================



    [Fact]

    public async Task CreateReservation_EmptyRestaurantId_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        request.RestaurantId = Guid.Empty;



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 3

    // Past reservation date

    // =========================================================



    [Fact]

    public async Task CreateReservation_PastDate_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        request.ReservationDate =

            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 4

    // Missing time slot

    // =========================================================



    [Fact]

    public async Task CreateReservation_EmptyTimeSlot_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        request.TimeSlot = string.Empty;



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 5

    // Party size = 0

    // =========================================================



    [Fact]

    public async Task CreateReservation_ZeroPartySize_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        request.PartySize = 0;



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 6

    // Negative party size

    // =========================================================



    [Fact]

    public async Task CreateReservation_NegativePartySize_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        request.PartySize = -5;



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 7

    // Restaurant does not exist

    // =========================================================



    [Fact]

    public async Task CreateReservation_RestaurantNotFound_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        _catalogServiceMock

            .Setup(x => x.GetRestaurantAsync(_restaurantId))

            .ReturnsAsync((CatalogRestaurantResponse?)null);



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 8

    // Restaurant inactive

    // =========================================================



    [Fact]

    public async Task CreateReservation_InactiveRestaurant_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        var restaurant = CreateValidRestaurant();



        restaurant.IsActive = false;



        _catalogServiceMock

            .Setup(x => x.GetRestaurantAsync(_restaurantId))

            .ReturnsAsync(restaurant);



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 9

    // Party size exceeds restaurant capacity

    // =========================================================



    [Fact]

    public async Task CreateReservation_PartySizeExceedsRestaurantCapacity_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        request.PartySize = 31;



        var restaurant = CreateValidRestaurant();



        restaurant.SeatingCapacity = 30;



        _catalogServiceMock

            .Setup(x => x.GetRestaurantAsync(_restaurantId))

            .ReturnsAsync(restaurant);



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 10

    // Availability service returns null

    // =========================================================



    [Fact]

    public async Task CreateReservation_AvailabilityNotFound_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        _catalogServiceMock

            .Setup(x => x.GetRestaurantAsync(_restaurantId))

            .ReturnsAsync(CreateValidRestaurant());



        _catalogServiceMock

            .Setup(x => x.GetAvailabilityAsync(

                _restaurantId,

                request.ReservationDate))

            .ReturnsAsync((CatalogAvailabilityResponse?)null);



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 11

    // Restaurant not operating

    // =========================================================



    [Fact]

    public async Task CreateReservation_NotOperatingDay_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        var availability = CreateValidAvailability();



        availability.IsOperatingDay = false;



        _catalogServiceMock

            .Setup(x => x.GetRestaurantAsync(_restaurantId))

            .ReturnsAsync(CreateValidRestaurant());



        _catalogServiceMock

            .Setup(x => x.GetAvailabilityAsync(

                _restaurantId,

                request.ReservationDate))

            .ReturnsAsync(availability);



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 12

    // Restaurant fully booked

    // =========================================================



    [Fact]

    public async Task CreateReservation_FullyBooked_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        var availability = CreateValidAvailability();



        availability.IsFullyBooked = true;



        _catalogServiceMock

            .Setup(x => x.GetRestaurantAsync(_restaurantId))

            .ReturnsAsync(CreateValidRestaurant());



        _catalogServiceMock

            .Setup(x => x.GetAvailabilityAsync(

                _restaurantId,

                request.ReservationDate))

            .ReturnsAsync(availability);



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 13

    // Selected time slot does not exist

    // =========================================================



    [Fact]

    public async Task CreateReservation_InvalidTimeSlot_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        request.TimeSlot = "05:00 PM - 06:00 PM";



        _catalogServiceMock

            .Setup(x => x.GetRestaurantAsync(_restaurantId))

            .ReturnsAsync(CreateValidRestaurant());



        _catalogServiceMock

            .Setup(x => x.GetAvailabilityAsync(

                _restaurantId,

                request.ReservationDate))

            .ReturnsAsync(CreateValidAvailability());



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 14

    // Selected slot itself is fully booked

    // =========================================================



    [Fact]

    public async Task CreateReservation_SelectedSlotFullyBooked_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        var availability = CreateValidAvailability();



        availability.Slots[0].IsFullyBooked = true;

        availability.Slots[0].RemainingCapacity = 0;



        _catalogServiceMock

            .Setup(x => x.GetRestaurantAsync(_restaurantId))

            .ReturnsAsync(CreateValidRestaurant());



        _catalogServiceMock

            .Setup(x => x.GetAvailabilityAsync(

                _restaurantId,

                request.ReservationDate))

            .ReturnsAsync(availability);



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 15

    // Party size exceeds remaining slot capacity

    // =========================================================



    [Fact]

    public async Task CreateReservation_InsufficientRemainingCapacity_ReturnsBadRequest()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        request.PartySize = 6;



        var availability = CreateValidAvailability();



        availability.Slots[0].RemainingCapacity = 5;



        _catalogServiceMock

            .Setup(x => x.GetRestaurantAsync(_restaurantId))

            .ReturnsAsync(CreateValidRestaurant());



        _catalogServiceMock

            .Setup(x => x.GetAvailabilityAsync(

                _restaurantId,

                request.ReservationDate))

            .ReturnsAsync(availability);



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<BadRequestObjectResult>(result);

    }



    // =========================================================

    // TEST 16

    // Capacity changed before reservation could be saved

    // =========================================================



    [Fact]

    public async Task CreateReservation_CapacityReservationFails_ReturnsConflict()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        _catalogServiceMock

            .Setup(x => x.GetRestaurantAsync(_restaurantId))

            .ReturnsAsync(CreateValidRestaurant());



        _catalogServiceMock

            .Setup(x => x.GetAvailabilityAsync(

                _restaurantId,

                request.ReservationDate))

            .ReturnsAsync(CreateValidAvailability());



        _catalogServiceMock

            .Setup(x => x.ReserveCapacityAsync(

                _restaurantId,

                request.ReservationDate,

                request.TimeSlot,

                request.PartySize))

            .ReturnsAsync(false);



        var result =

            await controller.CreateReservation(request);



        Assert.IsType<ConflictObjectResult>(result);



        Assert.Empty(context.RestaurantReservations);

    }



    // =========================================================

    // TEST 17

    // Successful reservation

    // =========================================================



    [Fact]

    public async Task CreateReservation_ValidRequest_ReturnsOk()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        SetupSuccessfulCatalog();



        var result =

            await controller.CreateReservation(request);



        var okResult =

            Assert.IsType<OkObjectResult>(result);



        Assert.NotNull(okResult.Value);

    }



    // =========================================================

    // TEST 18

    // Successful reservation stored in database

    // =========================================================



    [Fact]

    public async Task CreateReservation_ValidRequest_SavesReservationToDatabase()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        SetupSuccessfulCatalog();



        await controller.CreateReservation(request);



        var reservation =

            await context.RestaurantReservations

                .SingleAsync();



        Assert.Equal(_visitorId, reservation.VisitorId);

        Assert.Equal(_restaurantId, reservation.RestaurantId);



        Assert.Equal(

            "Test Restaurant",

            reservation.RestaurantName);



        Assert.Equal(

            request.ReservationDate,

            reservation.ReservationDate);



        Assert.Equal(

            request.TimeSlot,

            reservation.TimeSlot);



        Assert.Equal(

            request.PartySize,

            reservation.PartySize);



        Assert.Equal(2000m, reservation.PricePerPerson);

        Assert.Equal(8000m, reservation.TotalPrice);

    }



    // =========================================================

    // TEST 19

    // New reservation status should be Confirmed

    // =========================================================



    [Fact]

    public async Task CreateReservation_ValidRequest_StatusIsConfirmed()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        SetupSuccessfulCatalog();



        await controller.CreateReservation(request);



        var reservation =

            await context.RestaurantReservations

                .SingleAsync();



        Assert.Equal(

            ReservationStatus.Confirmed,

            reservation.Status);

    }



    // =========================================================

    // TEST 20

    // Correct response data including pricing

    // =========================================================



    [Fact]

    public async Task CreateReservation_ValidRequest_ReturnsCorrectResponse()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        SetupSuccessfulCatalog();



        var result =

            await controller.CreateReservation(request);



        var okResult =

            Assert.IsType<OkObjectResult>(result);



        var response =

            Assert.IsType<RestaurantReservationResponse>(

                okResult.Value);



        Assert.Equal(_restaurantId, response.RestaurantId);



        Assert.Equal(

            "Test Restaurant",

            response.RestaurantName);



        Assert.Equal(

            request.ReservationDate,

            response.ReservationDate);



        Assert.Equal(

            request.TimeSlot,

            response.TimeSlot);



        Assert.Equal(

            request.PartySize,

            response.PartySize);



        Assert.Equal(

            ReservationStatus.Confirmed,

            response.Status);



        Assert.Equal(2000m, response.PricePerPerson);

        Assert.Equal(8000m, response.TotalPrice);

    }



    // =========================================================

    // TEST 21

    // Capacity service called correctly

    // =========================================================



    [Fact]

    public async Task CreateReservation_ValidRequest_CallsReserveCapacity()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        SetupSuccessfulCatalog();



        await controller.CreateReservation(request);



        _catalogServiceMock.Verify(

            x => x.ReserveCapacityAsync(

                _restaurantId,

                request.ReservationDate,

                request.TimeSlot,

                request.PartySize),

            Times.Once);

    }



    // =========================================================

    // TEST 22

    // Invalid request must not reserve capacity

    // =========================================================



    [Fact]

    public async Task CreateReservation_InvalidPartySize_DoesNotReserveCapacity()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var request = CreateValidRequest();



        request.PartySize = 0;



        await controller.CreateReservation(request);



        _catalogServiceMock.Verify(

            x => x.ReserveCapacityAsync(

                It.IsAny<Guid>(),

                It.IsAny<DateOnly>(),

                It.IsAny<string>(),

                It.IsAny<int>()),

            Times.Never);

    }



    // =========================================================

    // TEST 23

    // GetMyReservations invalid authentication

    // =========================================================



    [Fact]

    public async Task GetMyReservations_InvalidAuthentication_ReturnsUnauthorized()

    {

        await using var context = CreateDbContext();



        var controller = CreateController(context);



        var result =

            await controller.GetMyReservations();



        Assert.IsType<UnauthorizedObjectResult>(result);

    }



    // =========================================================

    // TEST 24

    // GetMyReservations only returns current visitor data

    // =========================================================



    [Fact]

    public async Task GetMyReservations_ReturnsOnlyCurrentVisitorsReservations()

    {

        await using var context = CreateDbContext();



        var otherVisitorId = Guid.NewGuid();



        context.RestaurantReservations.AddRange(

            new RestaurantReservation

            {

                Id = Guid.NewGuid(),

                VisitorId = _visitorId,

                RestaurantId = _restaurantId,

                RestaurantName = "My Restaurant",

                ReservationDate =

                    DateOnly.FromDateTime(

                        DateTime.UtcNow.AddDays(1)),

                TimeSlot = "09:00 AM - 10:00 AM",

                PartySize = 4,

                Status = ReservationStatus.Confirmed,

                CreatedAt = DateTime.UtcNow,

                UpdatedAt = DateTime.UtcNow

            },



            new RestaurantReservation

            {

                Id = Guid.NewGuid(),

                VisitorId = otherVisitorId,

                RestaurantId = Guid.NewGuid(),

                RestaurantName = "Other Restaurant",

                ReservationDate =

                    DateOnly.FromDateTime(

                        DateTime.UtcNow.AddDays(1)),

                TimeSlot = "10:00 AM - 11:00 AM",

                PartySize = 2,

                Status = ReservationStatus.Confirmed,

                CreatedAt = DateTime.UtcNow,

                UpdatedAt = DateTime.UtcNow

            });



        await context.SaveChangesAsync();



        var controller =

            CreateController(context, _visitorId);



        var result =

            await controller.GetMyReservations();



        var okResult =

            Assert.IsType<OkObjectResult>(result);



        var reservations =

            Assert.IsAssignableFrom<

                IEnumerable<RestaurantReservationListResponse>>(

                    okResult.Value);



        var list = reservations.ToList();



        Assert.Single(list);



        Assert.Equal(

            "My Restaurant",

            list[0].ServiceName);



        Assert.Equal(

            "Restaurant Reservation",

            list[0].BookingType);



        Assert.Equal(

            _restaurantId,

            list[0].RestaurantId);



        Assert.Equal(

            4,

            list[0].PartySize);



        Assert.Equal(

            "Confirmed",

            list[0].Status);

    }



    // =========================================================

    // TEST 25

    // GetMyReservations returns empty list

    // =========================================================



    [Fact]

    public async Task GetMyReservations_NoReservations_ReturnsEmptyList()

    {

        await using var context = CreateDbContext();



        var controller =

            CreateController(context, _visitorId);



        var result =

            await controller.GetMyReservations();



        var okResult =

            Assert.IsType<OkObjectResult>(result);



        var reservations =

            Assert.IsAssignableFrom<

                IEnumerable<RestaurantReservationListResponse>>(

                    okResult.Value);



        Assert.Empty(reservations);

    }


    // =========================================================
    // STORY 9.2 - RESTAURANT CANCELLATION / REFUND TESTS
    // =========================================================

    [Fact]
    public async Task CancelReservation_MoreThan48HoursBefore_ReturnsFullRefund()
    {
        await using var context = CreateDbContext();
        var reservation = CreateStoredReservation(_visitorId, DateTime.Now.AddDays(5));
        context.RestaurantReservations.Add(reservation);
        await context.SaveChangesAsync();
        var controller = CreateController(context, _visitorId);

        var result = await controller.CancelReservation(reservation.Id, new CancelRestaurantReservationRequest { Reason = "Plans changed" });

        Assert.IsType<OkObjectResult>(result);
        var saved = await context.RestaurantReservations.SingleAsync(x => x.Id == reservation.Id);
        Assert.Equal(ReservationStatus.Cancelled, saved.Status);
        Assert.Equal(100m, saved.RefundPercentage);
        Assert.Equal(saved.TotalPrice, saved.RefundAmount);
        Assert.Equal("Plans changed", saved.CancellationReason);
        Assert.NotNull(saved.CancelledAt);
        Assert.NotNull(saved.RefundedAt);
    }

    [Fact]
    public async Task CancelReservation_Between24And48HoursBefore_ReturnsHalfRefund()
    {
        await using var context = CreateDbContext();
        var reservation = CreateStoredReservation(_visitorId, DateTime.Now.AddHours(36));
        context.RestaurantReservations.Add(reservation);
        await context.SaveChangesAsync();
        var controller = CreateController(context, _visitorId);

        var result = await controller.CancelReservation(reservation.Id, new CancelRestaurantReservationRequest { Reason = "Schedule changed" });

        Assert.IsType<OkObjectResult>(result);
        var saved = await context.RestaurantReservations.SingleAsync(x => x.Id == reservation.Id);
        Assert.Equal(ReservationStatus.Cancelled, saved.Status);
        Assert.Equal(50m, saved.RefundPercentage);
        Assert.Equal(saved.TotalPrice * 0.5m, saved.RefundAmount);
    }

    [Fact]
    public async Task CancelReservation_LessThan24HoursBefore_ReturnsBadRequestAndNoRefund()
    {
        await using var context = CreateDbContext();
        var reservation = CreateStoredReservation(_visitorId, DateTime.Now.AddHours(12));
        context.RestaurantReservations.Add(reservation);
        await context.SaveChangesAsync();
        var controller = CreateController(context, _visitorId);

        var result = await controller.CancelReservation(reservation.Id, new CancelRestaurantReservationRequest());

        Assert.IsType<BadRequestObjectResult>(result);
        var saved = await context.RestaurantReservations.SingleAsync(x => x.Id == reservation.Id);
        Assert.Equal(ReservationStatus.Confirmed, saved.Status);
        Assert.Equal(0m, saved.RefundAmount);
        Assert.Null(saved.CancelledAt);
        Assert.Null(saved.RefundedAt);
    }

    [Fact]
    public async Task CancelReservation_PastReservation_ReturnsBadRequest()
    {
        await using var context = CreateDbContext();
        var reservation = CreateStoredReservation(_visitorId, DateTime.Now.AddDays(-1));
        context.RestaurantReservations.Add(reservation);
        await context.SaveChangesAsync();
        var controller = CreateController(context, _visitorId);

        var result = await controller.CancelReservation(reservation.Id, new CancelRestaurantReservationRequest());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CancelReservation_AlreadyCancelled_ReturnsBadRequestAndDoesNotPublishAgain()
    {
        await using var context = CreateDbContext();
        var reservation = CreateStoredReservation(_visitorId, DateTime.Now.AddDays(5));
        reservation.Status = ReservationStatus.Cancelled;
        reservation.RefundPercentage = 100m;
        reservation.RefundAmount = reservation.TotalPrice;
        context.RestaurantReservations.Add(reservation);
        await context.SaveChangesAsync();
        var controller = CreateController(context, _visitorId);

        var result = await controller.CancelReservation(reservation.Id, new CancelRestaurantReservationRequest());

        Assert.IsType<BadRequestObjectResult>(result);
        _kafkaProducerMock.Verify(x => x.PublishAsync<BookingService.Events.BookingCanceledEvent>("booking.canceled", It.IsAny<string?>(), It.IsAny<BookingService.Events.BookingCanceledEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelReservation_OtherVisitorsReservation_ReturnsNotFound()
    {
        await using var context = CreateDbContext();
        var reservation = CreateStoredReservation(Guid.NewGuid(), DateTime.Now.AddDays(5));
        context.RestaurantReservations.Add(reservation);
        await context.SaveChangesAsync();
        var controller = CreateController(context, _visitorId);

        var result = await controller.CancelReservation(reservation.Id, new CancelRestaurantReservationRequest());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CancelReservation_ReservationNotFound_ReturnsNotFound()
    {
        await using var context = CreateDbContext();
        var controller = CreateController(context, _visitorId);

        var result = await controller.CancelReservation(Guid.NewGuid(), new CancelRestaurantReservationRequest());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CancelReservation_InvalidAuthentication_ReturnsUnauthorized()
    {
        await using var context = CreateDbContext();
        var controller = CreateController(context);

        var result = await controller.CancelReservation(Guid.NewGuid(), new CancelRestaurantReservationRequest());

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task CancelReservation_ValidReservation_PublishesCapacityRestorationEvent()
    {
        await using var context = CreateDbContext();
        var reservation = CreateStoredReservation(_visitorId, DateTime.Now.AddDays(5));
        reservation.PartySize = 4;
        context.RestaurantReservations.Add(reservation);
        await context.SaveChangesAsync();
        var controller = CreateController(context, _visitorId);

        var result = await controller.CancelReservation(reservation.Id, new CancelRestaurantReservationRequest { Reason = "Cancel" });

        Assert.IsType<OkObjectResult>(result);
        _kafkaProducerMock.Verify(x => x.PublishAsync<BookingService.Events.BookingCanceledEvent>(
            "booking.canceled",
            reservation.Id.ToString(),
            It.Is<BookingService.Events.BookingCanceledEvent>(e =>
                e.BookingId == reservation.Id &&
                e.ListingId == reservation.RestaurantId &&
                e.ListingType == "Restaurant" &&
                e.BookingDate == reservation.ReservationDate.ToString("yyyy-MM-dd") &&
                e.TimeSlot == reservation.TimeSlot &&
                e.ParticipantCount == 4),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelReservation_SecondCancellation_DoesNotChangeRefundOrPublishTwice()
    {
        await using var context = CreateDbContext();
        var reservation = CreateStoredReservation(_visitorId, DateTime.Now.AddDays(5));
        context.RestaurantReservations.Add(reservation);
        await context.SaveChangesAsync();
        var controller = CreateController(context, _visitorId);

        var first = await controller.CancelReservation(reservation.Id, new CancelRestaurantReservationRequest { Reason = "First request" });
        Assert.IsType<OkObjectResult>(first);
        var firstRefund = reservation.RefundAmount;
        var firstCancelledAt = reservation.CancelledAt;

        var second = await controller.CancelReservation(reservation.Id, new CancelRestaurantReservationRequest { Reason = "Second request" });

        Assert.IsType<BadRequestObjectResult>(second);
        Assert.Equal(firstRefund, reservation.RefundAmount);
        Assert.Equal(firstCancelledAt, reservation.CancelledAt);
        Assert.Equal("First request", reservation.CancellationReason);
        _kafkaProducerMock.Verify(x => x.PublishAsync<BookingService.Events.BookingCanceledEvent>("booking.canceled", It.IsAny<string?>(), It.IsAny<BookingService.Events.BookingCanceledEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private RestaurantReservation CreateStoredReservation(Guid visitorId, DateTime startDateTime)
    {
        return new RestaurantReservation
        {
            Id = Guid.NewGuid(),
            VisitorId = visitorId,
            RestaurantId = _restaurantId,
            RestaurantName = "Cancellation Test Restaurant",
            ReservationDate = DateOnly.FromDateTime(startDateTime),
            TimeSlot = startDateTime.ToString("hh:mm tt") + " - " + startDateTime.AddHours(1).ToString("hh:mm tt"),
            PartySize = 2,
            PricePerPerson = 2000m,
            TotalPrice = 4000m,
            Status = ReservationStatus.Confirmed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

}
