using BookingService.Controllers;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace BookingService.Tests;

public class UserBookingsControllerTests
{
    // =====================================================
    // TEST 1
    // Visitor can view their own experience booking
    // =====================================================

    [Fact]
    public async Task GetMyBookings_ReturnsExperienceBooking_ForLoggedInVisitor()
    {
        // Arrange
        var visitorId = Guid.NewGuid();

        await using var context = CreateContext();

        context.Bookings.Add(
            CreateBooking(
                visitorId,
                "Sri Lankan Cooking Class"));

        await context.SaveChangesAsync();

        var controller =
            CreateController(context, visitorId);

        // Act
        var result =
            await controller.GetMyBookingsAndReservations();

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<UserBookingResponse>>(
                okResult.Value);

        var list = bookings.ToList();

        Assert.Single(list);

        Assert.Equal(
            "Experience Booking",
            list[0].BookingType);

        Assert.Equal(
            "Sri Lankan Cooking Class",
            list[0].ServiceName);

        Assert.Equal(2, list[0].PeopleCount);
        Assert.Equal(13000m, list[0].TotalAmount);
    }


    // =====================================================
    // TEST 2
    // Visitor can view their own restaurant reservation
    // =====================================================

    [Fact]
    public async Task GetMyBookings_ReturnsRestaurantReservation_ForLoggedInVisitor()
    {
        // Arrange
        var visitorId = Guid.NewGuid();

        await using var context = CreateContext();

        context.RestaurantReservations.Add(
            CreateReservation(
                visitorId,
                "Ceylon Restaurant"));

        await context.SaveChangesAsync();

        var controller =
            CreateController(context, visitorId);

        // Act
        var result =
            await controller.GetMyBookingsAndReservations();

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<UserBookingResponse>>(
                okResult.Value);

        var list = bookings.ToList();

        Assert.Single(list);

        Assert.Equal(
            "Restaurant Reservation",
            list[0].BookingType);

        Assert.Equal(
            "Ceylon Restaurant",
            list[0].ServiceName);

        Assert.Equal(3, list[0].PeopleCount);
        Assert.Equal(6000m, list[0].TotalAmount);
    }


    // =====================================================
    // TEST 3
    // Experience bookings and restaurant reservations
    // are returned together in one unified list
    // =====================================================

    [Fact]
    public async Task GetMyBookings_ReturnsUnifiedBookingList()
    {
        // Arrange
        var visitorId = Guid.NewGuid();

        await using var context = CreateContext();

        context.Bookings.Add(
            CreateBooking(
                visitorId,
                "Tea Estate Experience"));

        context.RestaurantReservations.Add(
            CreateReservation(
                visitorId,
                "Ocean View Restaurant"));

        await context.SaveChangesAsync();

        var controller =
            CreateController(context, visitorId);

        // Act
        var result =
            await controller.GetMyBookingsAndReservations();

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<UserBookingResponse>>(
                okResult.Value);

        var list = bookings.ToList();

        Assert.Equal(2, list.Count);

        Assert.Contains(
            list,
            x => x.BookingType == "Experience Booking");

        Assert.Contains(
            list,
            x => x.BookingType == "Restaurant Reservation");

        Assert.Contains(
            list,
            x => x.ServiceName == "Tea Estate Experience");

        Assert.Contains(
            list,
            x => x.ServiceName == "Ocean View Restaurant");
    }


    // =====================================================
    // TEST 4
    // Visitor must not see another visitor's bookings
    // or restaurant reservations
    // =====================================================

    [Fact]
    public async Task GetMyBookings_DoesNotReturnAnotherVisitorsBookings()
    {
        // Arrange
        var loggedInVisitorId = Guid.NewGuid();
        var anotherVisitorId = Guid.NewGuid();

        await using var context = CreateContext();

        // Logged-in visitor's booking
        context.Bookings.Add(
            CreateBooking(
                loggedInVisitorId,
                "My Experience"));

        // Another visitor's experience booking
        context.Bookings.Add(
            CreateBooking(
                anotherVisitorId,
                "Another Visitor Experience"));

        // Another visitor's restaurant reservation
        context.RestaurantReservations.Add(
            CreateReservation(
                anotherVisitorId,
                "Another Visitor Restaurant"));

        await context.SaveChangesAsync();

        var controller =
            CreateController(
                context,
                loggedInVisitorId);

        // Act
        var result =
            await controller.GetMyBookingsAndReservations();

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<UserBookingResponse>>(
                okResult.Value);

        var list = bookings.ToList();

        Assert.Single(list);

        Assert.Equal(
            "My Experience",
            list[0].ServiceName);

        Assert.DoesNotContain(
            list,
            x => x.ServiceName ==
                 "Another Visitor Experience");

        Assert.DoesNotContain(
            list,
            x => x.ServiceName ==
                 "Another Visitor Restaurant");
    }


    // =====================================================
    // TEST 5
    // Visitor with no bookings receives an empty list
    // =====================================================

    [Fact]
    public async Task GetMyBookings_ReturnsEmptyList_WhenVisitorHasNoBookings()
    {
        // Arrange
        var visitorId = Guid.NewGuid();

        await using var context = CreateContext();

        var controller =
            CreateController(context, visitorId);

        // Act
        var result =
            await controller.GetMyBookingsAndReservations();

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<UserBookingResponse>>(
                okResult.Value);

        Assert.Empty(bookings);
    }


    // =====================================================
    // TEST 6
    // Experience booking returns payment information
    // =====================================================

    [Fact]
    public async Task GetMyBookings_ExperienceBooking_ReturnsPaymentStatus()
    {
        // Arrange
        var visitorId = Guid.NewGuid();

        await using var context = CreateContext();

        context.Bookings.Add(
            CreateBooking(
                visitorId,
                "Wildlife Experience"));

        await context.SaveChangesAsync();

        var controller =
            CreateController(context, visitorId);

        // Act
        var result =
            await controller.GetMyBookingsAndReservations();

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<UserBookingResponse>>(
                okResult.Value);

        var booking =
            Assert.Single(bookings);

        Assert.Equal(
            "Experience Booking",
            booking.BookingType);

        Assert.Equal(
            "Unpaid",
            booking.PaymentStatus);

        Assert.Equal(
            6500m,
            booking.UnitPrice);

        Assert.Equal(
            13000m,
            booking.TotalAmount);
    }


    // =====================================================
    // TEST 7
    // Restaurant reservation must not return a misleading
    // payment status
    // =====================================================

    [Fact]
    public async Task GetMyBookings_RestaurantReservation_HasNullPaymentStatus()
    {
        // Arrange
        var visitorId = Guid.NewGuid();

        await using var context = CreateContext();

        context.RestaurantReservations.Add(
            CreateReservation(
                visitorId,
                "Traditional Restaurant"));

        await context.SaveChangesAsync();

        var controller =
            CreateController(context, visitorId);

        // Act
        var result =
            await controller.GetMyBookingsAndReservations();

        // Assert
        var okResult =
            Assert.IsType<OkObjectResult>(result);

        var bookings =
            Assert.IsAssignableFrom<IEnumerable<UserBookingResponse>>(
                okResult.Value);

        var reservation =
            Assert.Single(bookings);

        Assert.Equal(
            "Restaurant Reservation",
            reservation.BookingType);

        Assert.Null(
            reservation.PaymentStatus);

        Assert.Equal(
            2000m,
            reservation.UnitPrice);

        Assert.Equal(
            6000m,
            reservation.TotalAmount);
    }


    // =====================================================
    // TEST 8
    // Missing visitor identity returns Unauthorized
    // =====================================================

    [Fact]
    public async Task GetMyBookings_ReturnsUnauthorized_WhenVisitorIdIsMissing()
    {
        // Arrange
        await using var context = CreateContext();

        var controller =
            new UserBookingsController(context);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext()
            };

        // Act
        var result =
            await controller.GetMyBookingsAndReservations();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(
            result);
    }


    // =====================================================
    // TEST HELPERS
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


    private static UserBookingsController CreateController(
        BookingDbContext context,
        Guid visitorId)
    {
        var controller =
            new UserBookingsController(context);

        var claims =
            new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    visitorId.ToString()),

                new(
                    ClaimTypes.Role,
                    "Visitor")
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

        return controller;
    }


    private static Booking CreateBooking(
        Guid visitorId,
        string title)
    {
        return new Booking
        {
            Id = Guid.NewGuid(),

            VisitorId = visitorId,

            ListingId = Guid.NewGuid(),

            ListingTitle = title,

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
        Guid visitorId,
        string restaurantName)
    {
        return new RestaurantReservation
        {
            Id = Guid.NewGuid(),

            VisitorId = visitorId,

            RestaurantId = Guid.NewGuid(),

            RestaurantName =
                restaurantName,

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