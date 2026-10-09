using System.Security.Claims;
using BookingService.Controllers;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Shared.Kafka;

namespace BookingService.Tests;

public class RestaurantReservationTimeTests
{
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-09-29T11:26:00Z");
    }

    private static ControllerContext Visitor(Guid id) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString())
            }, "test"))
        }
    };

    [Theory]
    [InlineData("2026-09-29", "09:00 AM - 10:00 AM", false)]
    [InlineData("2026-09-29", "12:00 PM - 01:00 PM", false)]
    [InlineData("2026-09-29", "04:00 PM - 05:00 PM", false)]
    [InlineData("2026-09-29", "04:56 PM - 05:56 PM", false)]
    [InlineData("2026-09-29", "05:00 PM - 06:00 PM", true)]
    [InlineData("2026-09-28", "05:00 PM - 06:00 PM", false)]
    [InlineData("2026-09-30", "09:00 AM - 10:00 AM", true)]
    [InlineData("2026-09-30", "00:00 - 01:00", true)]
    [InlineData("2026-09-30", "invalid", false)]
    public async Task CreationRejectsInvalidTimesBeforeSideEffects(string dateText, string slot, bool accepted)
    {
        await using var db = new BookingDbContext(new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var restaurantId = Guid.NewGuid();
        var date = DateOnly.Parse(dateText);
        var catalog = new Mock<ICatalogService>();
        var kafka = new Mock<IKafkaProducer>();
        catalog.Setup(c => c.GetRestaurantAsync(restaurantId)).ReturnsAsync(new CatalogRestaurantResponse
        {
            ProviderUserId = Guid.NewGuid(),
            Id = restaurantId, Name = "Restaurant", IsActive = true, SeatingCapacity = 10, PricePerPerson = 100
        });
        catalog.Setup(c => c.GetAvailabilityAsync(restaurantId, date)).ReturnsAsync(new CatalogAvailabilityResponse
        {
            IsOperatingDay = true,
            Slots = new() { new CatalogAvailabilitySlot { TimeSlot = slot, RemainingCapacity = 10 } }
        });
        catalog.Setup(c => c.ReserveCapacityAsync(restaurantId, date, slot, 2)).ReturnsAsync(true);
        var controller = new ReservationsController(db, catalog.Object, kafka.Object, new FixedClock())
        {
            ControllerContext = Visitor(Guid.NewGuid())
        };
        var result = await controller.CreateReservation(new CreateRestaurantReservationRequest
        {
            RestaurantId = restaurantId, ReservationDate = date, TimeSlot = slot, PartySize = 2
        });
        if (accepted)
        {
            Assert.IsType<OkObjectResult>(result);
            Assert.Single(db.RestaurantReservations);
        }
        else
        {
            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(db.RestaurantReservations);
            Assert.Empty(db.PaymentTransactions);
            kafka.VerifyNoOtherCalls();
        }
        catalog.Verify(c => c.ReserveCapacityAsync(restaurantId, date, slot, 2),
            accepted ? Times.Once() : Times.Never());
    }

    [Theory]
    [InlineData("2026-09-29", "11:00 PM - 11:30 PM", false)]
    [InlineData("2026-09-30", "12:00 AM - 01:00 AM", false)]
    [InlineData("2026-09-30", "01:00 AM - 02:00 AM", true)]
    public void ValidationUsesColomboDateAcrossUtcMidnight(string date, string slot, bool valid)
    {
        // September 30, 00:30 in Sri Lanka, still September 29 in UTC.
        var error = RestaurantReservationTime.Validate(DateOnly.Parse(date), slot,
            DateTimeOffset.Parse("2026-09-29T19:00:00Z"));
        Assert.Equal(valid, error == null);
    }

    [Fact]
    public async Task CheckoutRejectsPastRestaurantWithoutCreatingTransaction()
    {
        await using var db = new BookingDbContext(new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var visitor = Guid.NewGuid();
        var reservation = new RestaurantReservation
        {
            Id = Guid.NewGuid(), VisitorId = visitor,
            ReservationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            TimeSlot = "09:00 AM - 10:00 AM", CreatedAt = DateTime.UtcNow,
            Status = ReservationStatus.PendingPayment
        };
        db.RestaurantReservations.Add(reservation);
        await db.SaveChangesAsync();
        var kafka = new Mock<IKafkaProducer>();
        var controller = new PaymentsController(db, kafka.Object, new ConfigurationBuilder().Build())
        {
            ControllerContext = Visitor(visitor)
        };
        var result = await controller.CreateCheckoutSession(new CreatePaymentRequest
        {
            BookingId = reservation.Id, BookingType = "Restaurant"
        });
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.PaymentTransactions);
        kafka.VerifyNoOtherCalls();
    }
}
