using BookingService.Controllers;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Events;
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

public class BookingTests
{
    // =========================================================
    // TEST CASE 1
    // Booking can be saved with PendingPayment and Unpaid status
    // =========================================================

    [Fact]
    public async Task Booking_CanBeSaved_WithPendingPaymentStatus()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new BookingDbContext(options);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            VisitorId = Guid.NewGuid(),
            ListingId = Guid.NewGuid(),
            ListingTitle = "Test Experience",
            ListingType = "Experience",
            BookingDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(1)),
            TimeSlot = "08:00 AM - 11:00 AM",
            ParticipantCount = 2,
            UnitPrice = 5000m,
            TotalAmount = 10000m,
            Status = BookingStatus.PendingPayment,
            PaymentStatus = PaymentStatus.Unpaid,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var savedBooking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == booking.Id);

        // Assert
        Assert.NotNull(savedBooking);
        Assert.Equal(
            BookingStatus.PendingPayment,
            savedBooking.Status);
        Assert.Equal(
            PaymentStatus.Unpaid,
            savedBooking.PaymentStatus);
        Assert.Equal(2, savedBooking.ParticipantCount);
        Assert.Equal(5000m, savedBooking.UnitPrice);
        Assert.Equal(10000m, savedBooking.TotalAmount);
    }


    // =========================================================
    // TEST CASE 2
    // Participant count = 0 should be rejected
    // =========================================================

    [Fact]
    public async Task CreateBooking_ParticipantCountZero_ReturnsBadRequest()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogService = CreateCatalogService();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var controller = new BookingsController(
            context,
            catalogService,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = Guid.NewGuid(),
            BookingDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(1)),
            TimeSlot = "08:00 AM - 11:00 AM",
            ParticipantCount = 0
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Bookings);
    }


    // =========================================================
    // TEST CASE 3
    // Past booking date should be rejected
    // =========================================================

    [Fact]
    public async Task CreateBooking_PastDate_ReturnsBadRequest()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogService = CreateCatalogService();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var controller = new BookingsController(
            context,
            catalogService,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = Guid.NewGuid(),
            BookingDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(-1)),
            TimeSlot = "08:00 AM - 11:00 AM",
            ParticipantCount = 2
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Bookings);
    }


    // =========================================================
    // TEST CASE 4
    // Empty ListingId should be rejected
    // =========================================================

    [Fact]
    public async Task CreateBooking_EmptyListingId_ReturnsBadRequest()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogService = CreateCatalogService();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var controller = new BookingsController(
            context,
            catalogService,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = Guid.Empty,
            BookingDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(1)),
            TimeSlot = "08:00 AM - 11:00 AM",
            ParticipantCount = 2
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Bookings);
    }


    // =========================================================
    // TEST CASE 5
    // Empty time slot should be rejected
    // =========================================================

    [Fact]
    public async Task CreateBooking_EmptyTimeSlot_ReturnsBadRequest()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogService = CreateCatalogService();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var controller = new BookingsController(
            context,
            catalogService,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = Guid.NewGuid(),
            BookingDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(1)),
            TimeSlot = "",
            ParticipantCount = 2
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Bookings);
    }


    // =========================================================
    // TEST CASE 6
    // User without valid authentication should be rejected
    // =========================================================

    [Fact]
    public async Task CreateBooking_NoAuthenticatedVisitor_ReturnsUnauthorized()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogService = CreateCatalogService();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var controller = new BookingsController(
            context,
            catalogService,
            kafkaProducerMock.Object);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

        var request = new CreateBookingRequest
        {
            ListingId = Guid.NewGuid(),
            BookingDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(1)),
            TimeSlot = "08:00 AM - 11:00 AM",
            ParticipantCount = 2
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Empty(context.Bookings);
    }


    // =========================================================
    // TEST CASE 7
    // Experience/listing not found should be rejected
    // =========================================================

    [Fact]
    public async Task CreateBooking_ListingNotFound_ReturnsBadRequest()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogServiceMock = new Mock<ICatalogService>();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var listingId = Guid.NewGuid();

        catalogServiceMock
            .Setup(service =>
                service.GetListingAsync(listingId))
            .ReturnsAsync((CatalogListingResponse?)null);

        var controller = new BookingsController(
            context,
            catalogServiceMock.Object,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = listingId,
            BookingDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(1)),
            TimeSlot = "08:00 AM - 11:00 AM",
            ParticipantCount = 2
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Bookings);

        catalogServiceMock.Verify(
            service =>
                service.GetListingAsync(listingId),
            Times.Once);

        catalogServiceMock.Verify(
            service =>
                service.ReserveCapacityAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()),
            Times.Never);
    }


    // =========================================================
    // TEST CASE 8
    // Inactive experience should be rejected
    // =========================================================

    [Fact]
    public async Task CreateBooking_InactiveExperience_ReturnsBadRequest()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogServiceMock = new Mock<ICatalogService>();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var listingId = Guid.NewGuid();

        var listing = new CatalogListingResponse
        {
            Id = listingId,
            Title = "Inactive Test Experience",
            Price = 5000m,
            Unit = "per person",
            MaxParticipants = 10,
            IsActive = false
        };

        catalogServiceMock
            .Setup(service =>
                service.GetListingAsync(listingId))
            .ReturnsAsync(listing);

        var controller = new BookingsController(
            context,
            catalogServiceMock.Object,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = listingId,
            BookingDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(1)),
            TimeSlot = "08:00 AM - 11:00 AM",
            ParticipantCount = 2
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Bookings);

        catalogServiceMock.Verify(
            service =>
                service.GetListingAsync(listingId),
            Times.Once);

        catalogServiceMock.Verify(
            service =>
                service.ReserveCapacityAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()),
            Times.Never);
    }


    // =========================================================
    // TEST CASE 9
    // Participant count exceeding maximum should be rejected
    // =========================================================

    [Fact]
    public async Task CreateBooking_ParticipantCountExceedsMaximum_ReturnsBadRequest()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogServiceMock = new Mock<ICatalogService>();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var listingId = Guid.NewGuid();

        var listing = new CatalogListingResponse
        {
            Id = listingId,
            Title = "Test Experience",
            Price = 5000m,
            Unit = "per person",
            MaxParticipants = 5,
            IsActive = true
        };

        catalogServiceMock
            .Setup(service =>
                service.GetListingAsync(listingId))
            .ReturnsAsync(listing);

        var controller = new BookingsController(
            context,
            catalogServiceMock.Object,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = listingId,
            BookingDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(1)),
            TimeSlot = "08:00 AM - 11:00 AM",
            ParticipantCount = 6
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Bookings);

        catalogServiceMock.Verify(
            service =>
                service.GetAvailabilityAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateOnly>()),
            Times.Never);

        catalogServiceMock.Verify(
            service =>
                service.ReserveCapacityAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()),
            Times.Never);
    }


    // =========================================================
    // TEST CASE 10
    // Non-operating date should be rejected
    // =========================================================

    [Fact]
    public async Task CreateBooking_NonOperatingDay_ReturnsBadRequest()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogServiceMock = new Mock<ICatalogService>();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var listingId = Guid.NewGuid();

        var bookingDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(1));

        var listing = CreateActiveListing(listingId);

        catalogServiceMock
            .Setup(service =>
                service.GetListingAsync(listingId))
            .ReturnsAsync(listing);

        var availability = new CatalogAvailabilityResponse
        {
            ListingId = listingId,
            Date = bookingDate.ToString("yyyy-MM-dd"),
            IsOperatingDay = false,
            IsFullyBooked = false,
            Slots = new List<CatalogAvailabilitySlot>()
        };

        catalogServiceMock
            .Setup(service =>
                service.GetAvailabilityAsync(
                    listingId,
                    bookingDate))
            .ReturnsAsync(availability);

        var controller = new BookingsController(
            context,
            catalogServiceMock.Object,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = listingId,
            BookingDate = bookingDate,
            TimeSlot = "08:00 AM - 11:00 AM",
            ParticipantCount = 2
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Bookings);

        catalogServiceMock.Verify(
            service =>
                service.GetAvailabilityAsync(
                    listingId,
                    bookingDate),
            Times.Once);

        catalogServiceMock.Verify(
            service =>
                service.ReserveCapacityAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()),
            Times.Never);
    }


    // =========================================================
    // TEST CASE 11
    // Fully booked day should be rejected
    // =========================================================

    [Fact]
    public async Task CreateBooking_FullyBookedDay_ReturnsBadRequest()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogServiceMock = new Mock<ICatalogService>();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var listingId = Guid.NewGuid();

        var bookingDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(1));

        var listing = CreateActiveListing(listingId);

        catalogServiceMock
            .Setup(service =>
                service.GetListingAsync(listingId))
            .ReturnsAsync(listing);

        var availability = new CatalogAvailabilityResponse
        {
            ListingId = listingId,
            Date = bookingDate.ToString("yyyy-MM-dd"),
            IsOperatingDay = true,
            IsFullyBooked = true,
            Slots = new List<CatalogAvailabilitySlot>()
        };

        catalogServiceMock
            .Setup(service =>
                service.GetAvailabilityAsync(
                    listingId,
                    bookingDate))
            .ReturnsAsync(availability);

        var controller = new BookingsController(
            context,
            catalogServiceMock.Object,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = listingId,
            BookingDate = bookingDate,
            TimeSlot = "08:00 AM - 11:00 AM",
            ParticipantCount = 2
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Bookings);

        catalogServiceMock.Verify(
            service =>
                service.ReserveCapacityAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()),
            Times.Never);
    }


    // =========================================================
    // TEST CASE 12
    // Invalid/nonexistent time slot should be rejected
    // =========================================================

    [Fact]
    public async Task CreateBooking_InvalidTimeSlot_ReturnsBadRequest()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogServiceMock = new Mock<ICatalogService>();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var listingId = Guid.NewGuid();

        var bookingDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(1));

        var listing = CreateActiveListing(listingId);

        catalogServiceMock
            .Setup(service =>
                service.GetListingAsync(listingId))
            .ReturnsAsync(listing);

        var availability = CreateAvailableResponse(
            listingId,
            bookingDate,
            remainingCapacity: 5);

        catalogServiceMock
            .Setup(service =>
                service.GetAvailabilityAsync(
                    listingId,
                    bookingDate))
            .ReturnsAsync(availability);

        var controller = new BookingsController(
            context,
            catalogServiceMock.Object,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = listingId,
            BookingDate = bookingDate,
            TimeSlot = "01:00 PM - 04:00 PM",
            ParticipantCount = 2
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Bookings);

        catalogServiceMock.Verify(
            service =>
                service.ReserveCapacityAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()),
            Times.Never);
    }


    // =========================================================
    // TEST CASE 13
    // Insufficient remaining capacity should be rejected
    // =========================================================

    [Fact]
    public async Task CreateBooking_InsufficientCapacity_ReturnsBadRequest()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogServiceMock = new Mock<ICatalogService>();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var listingId = Guid.NewGuid();

        var bookingDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(1));

        var listing = CreateActiveListing(listingId);

        catalogServiceMock
            .Setup(service =>
                service.GetListingAsync(listingId))
            .ReturnsAsync(listing);

        var availability = CreateAvailableResponse(
            listingId,
            bookingDate,
            remainingCapacity: 1);

        catalogServiceMock
            .Setup(service =>
                service.GetAvailabilityAsync(
                    listingId,
                    bookingDate))
            .ReturnsAsync(availability);

        var controller = new BookingsController(
            context,
            catalogServiceMock.Object,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = listingId,
            BookingDate = bookingDate,
            TimeSlot = "08:00 AM - 11:00 AM",
            ParticipantCount = 2
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Bookings);

        catalogServiceMock.Verify(
            service =>
                service.ReserveCapacityAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()),
            Times.Never);
    }


    // =========================================================
    // TEST CASE 14
    // Valid booking should reserve capacity and be created
    // with correct total amount and initial statuses
    // =========================================================

    [Fact]
    public async Task CreateBooking_ValidRequest_CreatesBookingSuccessfully()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogServiceMock = new Mock<ICatalogService>();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var listingId = Guid.NewGuid();

        var bookingDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(1));

        const string timeSlot = "08:00 AM - 11:00 AM";
        const int participantCount = 2;

        var listing = CreateActiveListing(listingId);

        catalogServiceMock
            .Setup(service =>
                service.GetListingAsync(listingId))
            .ReturnsAsync(listing);

        var availability = CreateAvailableResponse(
            listingId,
            bookingDate,
            remainingCapacity: 5);

        catalogServiceMock
            .Setup(service =>
                service.GetAvailabilityAsync(
                    listingId,
                    bookingDate))
            .ReturnsAsync(availability);

        // IMPORTANT:
        // Capacity reservation succeeds.
        catalogServiceMock
            .Setup(service =>
                service.ReserveCapacityAsync(
                    listingId,
                    bookingDate,
                    timeSlot,
                    participantCount))
            .ReturnsAsync(true);

        var controller = new BookingsController(
            context,
            catalogServiceMock.Object,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = listingId,
            BookingDate = bookingDate,
            TimeSlot = timeSlot,
            ParticipantCount = participantCount
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        var createdResult =
            Assert.IsType<CreatedAtActionResult>(result);

        var createdBooking =
            Assert.IsType<Booking>(createdResult.Value);

        Assert.Equal(
            listingId,
            createdBooking.ListingId);

        Assert.Equal(
            "Test Experience",
            createdBooking.ListingTitle);

        Assert.Equal(
            bookingDate,
            createdBooking.BookingDate);

        Assert.Equal(
            timeSlot,
            createdBooking.TimeSlot);

        Assert.Equal(
            participantCount,
            createdBooking.ParticipantCount);

        // Price × participants
        Assert.Equal(
            5000m,
            createdBooking.UnitPrice);

        Assert.Equal(
            10000m,
            createdBooking.TotalAmount);

        // Story 7.1 initial statuses
        Assert.Equal(
            BookingStatus.PendingPayment,
            createdBooking.Status);

        Assert.Equal(
            PaymentStatus.Unpaid,
            createdBooking.PaymentStatus);

        // Booking must be stored
        var savedBooking =
            Assert.Single(context.Bookings);

        Assert.Equal(
            createdBooking.Id,
            savedBooking.Id);

        Assert.Equal(
            10000m,
            savedBooking.TotalAmount);

        Assert.Equal(
            BookingStatus.PendingPayment,
            savedBooking.Status);

        Assert.Equal(
            PaymentStatus.Unpaid,
            savedBooking.PaymentStatus);

        // Capacity must be reserved exactly once
        catalogServiceMock.Verify(
            service =>
                service.ReserveCapacityAsync(
                    listingId,
                    bookingDate,
                    timeSlot,
                    participantCount),
            Times.Once);
    }


    // =========================================================
    // TEST CASE 15
    // Successful booking should publish booking.created event
    // =========================================================

    [Fact]
    public async Task CreateBooking_ValidRequest_PublishesBookingCreatedEvent()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogServiceMock = new Mock<ICatalogService>();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var listingId = Guid.NewGuid();

        var bookingDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(1));

        const string timeSlot = "08:00 AM - 11:00 AM";
        const int participantCount = 2;

        var listing = CreateActiveListing(listingId);

        catalogServiceMock
            .Setup(service =>
                service.GetListingAsync(listingId))
            .ReturnsAsync(listing);

        var availability = CreateAvailableResponse(
            listingId,
            bookingDate,
            remainingCapacity: 5);

        catalogServiceMock
            .Setup(service =>
                service.GetAvailabilityAsync(
                    listingId,
                    bookingDate))
            .ReturnsAsync(availability);

        // Capacity reservation succeeds
        catalogServiceMock
            .Setup(service =>
                service.ReserveCapacityAsync(
                    listingId,
                    bookingDate,
                    timeSlot,
                    participantCount))
            .ReturnsAsync(true);

        kafkaProducerMock
            .Setup(producer =>
                producer.PublishAsync(
                    "booking.created",
                    It.IsAny<string?>(),
                    It.IsAny<BookingCreatedEvent>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new BookingsController(
            context,
            catalogServiceMock.Object,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = listingId,
            BookingDate = bookingDate,
            TimeSlot = timeSlot,
            ParticipantCount = participantCount
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);

        Assert.Single(context.Bookings);

        // Capacity must be reserved first
        catalogServiceMock.Verify(
            service =>
                service.ReserveCapacityAsync(
                    listingId,
                    bookingDate,
                    timeSlot,
                    participantCount),
            Times.Once);

        // booking.created must be published exactly once
        kafkaProducerMock.Verify(
            producer =>
                producer.PublishAsync(
                    "booking.created",
                    It.IsAny<string?>(),

                    It.Is<BookingCreatedEvent>(message =>
                        message.ListingId == listingId &&
                        message.BookingDate ==
                            bookingDate.ToString("yyyy-MM-dd") &&
                        message.TimeSlot == timeSlot &&
                        message.ParticipantCount ==
                            participantCount &&
                        message.TotalAmount == 10000m),

                    It.IsAny<CancellationToken>()),
            Times.Once);
    }


    // =========================================================
    // TEST CASE 16
    // Capacity reservation failure should prevent overbooking
    // =========================================================

    [Fact]
    public async Task CreateBooking_ReservationFails_ReturnsConflict()
    {
        // Arrange
        var options = CreateDatabaseOptions();

        await using var context = new BookingDbContext(options);

        var catalogServiceMock = new Mock<ICatalogService>();
        var kafkaProducerMock = new Mock<IKafkaProducer>();

        var listingId = Guid.NewGuid();

        var bookingDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(1));

        const string timeSlot = "08:00 AM - 11:00 AM";
        const int participantCount = 2;

        var listing = CreateActiveListing(listingId);

        catalogServiceMock
            .Setup(service =>
                service.GetListingAsync(listingId))
            .ReturnsAsync(listing);

        // Initial availability check says 2 places are available.
        var availability = CreateAvailableResponse(
            listingId,
            bookingDate,
            remainingCapacity: 2);

        catalogServiceMock
            .Setup(service =>
                service.GetAvailabilityAsync(
                    listingId,
                    bookingDate))
            .ReturnsAsync(availability);

        // Simulate another visitor taking the remaining capacity
        // between the availability check and final reservation.
        catalogServiceMock
            .Setup(service =>
                service.ReserveCapacityAsync(
                    listingId,
                    bookingDate,
                    timeSlot,
                    participantCount))
            .ReturnsAsync(false);

        var controller = new BookingsController(
            context,
            catalogServiceMock.Object,
            kafkaProducerMock.Object);

        SetAuthenticatedUser(controller);

        var request = new CreateBookingRequest
        {
            ListingId = listingId,
            BookingDate = bookingDate,
            TimeSlot = timeSlot,
            ParticipantCount = participantCount
        };

        // Act
        var result = await controller.CreateBooking(request);

        // Assert
        Assert.IsType<ConflictObjectResult>(result);

        // Booking must NOT be created
        Assert.Empty(context.Bookings);

        // Reservation was attempted exactly once
        catalogServiceMock.Verify(
            service =>
                service.ReserveCapacityAsync(
                    listingId,
                    bookingDate,
                    timeSlot,
                    participantCount),
            Times.Once);

        // Kafka event must NOT be published because
        // the booking was never created.
        kafkaProducerMock.Verify(
            producer =>
                producer.PublishAsync(
                    "booking.created",
                    It.IsAny<string?>(),
                    It.IsAny<BookingCreatedEvent>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }


    // =========================================================
    // HELPER METHOD
    // Creates unique InMemory database options
    // =========================================================

    private static DbContextOptions<BookingDbContext>
        CreateDatabaseOptions()
    {
        return new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }


    // =========================================================
    // HELPER METHOD
    // Creates CatalogService for early validation tests
    // =========================================================

    private static CatalogService CreateCatalogService()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost")
        };

        return new CatalogService(httpClient);
    }


    // =========================================================
    // HELPER METHOD
    // Creates an active test experience
    // =========================================================

    private static CatalogListingResponse CreateActiveListing(
        Guid listingId)
    {
        return new CatalogListingResponse
        {
            Id = listingId,
            Title = "Test Experience",
            Price = 5000m,
            Unit = "per person",
            MaxParticipants = 10,
            IsActive = true
        };
    }


    // =========================================================
    // HELPER METHOD
    // Creates valid availability for one time slot
    // =========================================================

    private static CatalogAvailabilityResponse
        CreateAvailableResponse(
            Guid listingId,
            DateOnly bookingDate,
            int remainingCapacity)
    {
        return new CatalogAvailabilityResponse
        {
            ListingId = listingId,
            Date = bookingDate.ToString("yyyy-MM-dd"),
            IsOperatingDay = true,
            IsFullyBooked = false,

            Slots = new List<CatalogAvailabilitySlot>
            {
                new CatalogAvailabilitySlot
                {
                    TimeSlot = "08:00 AM - 11:00 AM",
                    TotalCapacity = 5,
                    RemainingCapacity = remainingCapacity,
                    IsFullyBooked = remainingCapacity <= 0
                }
            }
        };
    }


    // =========================================================
    // HELPER METHOD
    // Creates a fake authenticated visitor
    // =========================================================

    private static void SetAuthenticatedUser(
        BookingsController controller)
    {
        var visitorId = Guid.NewGuid();

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                visitorId.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            "TestAuthentication");

        var user = new ClaimsPrincipal(identity);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User = user
                    }
            };
    }
}