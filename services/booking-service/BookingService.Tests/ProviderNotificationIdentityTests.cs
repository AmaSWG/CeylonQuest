using System.Security.Claims;
using BookingService.Controllers;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Events;
using BookingService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Moq;
using Shared.Kafka;

namespace BookingService.Tests;

public class ProviderNotificationIdentityTests
{
    [Fact]
    public void ProviderIdentityMigrationAddsNullableColumnsToAllBookingTables()
    {
        using var db = new BookingDbContext(new DbContextOptionsBuilder<BookingDbContext>()
            .UseMySql("Server=localhost;Database=unused;User=unused;Password=unused;",
                new MySqlServerVersion(new Version(8, 0, 30))).Options);
        var sql = db.GetService<IMigrator>().GenerateScript("20260929120000_AddCheckoutExpiration",
            "20261002120000_AddBookingProviderUserIds");
        foreach (var table in new[] { "Bookings", "RestaurantReservations", "AccommodationBookings" })
            Assert.Contains($"ALTER TABLE `{table}` ADD `ProviderUserId`", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("50788057-ab97-4b16-a657-dc0124355dc7")]
    public async Task CreationRequiresProviderIdentityAndPublishesIt(string? providerId)
    {
        await using var db = new BookingDbContext(new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var listingId = Guid.NewGuid();
        var visitorId = Guid.NewGuid();
        var identity = providerId is null ? (Guid?)null : Guid.Parse(providerId);
        var catalog = new Mock<ICatalogService>();
        catalog.Setup(c => c.GetListingAsync(listingId)).ReturnsAsync(new CatalogListingResponse
        {
            Id = listingId, ProviderUserId = identity, Title = "Test", Price = 100,
            MaxParticipants = 10, IsActive = true,
            ValidFrom = new DateTime(2026, 11, 1), ValidUntil = new DateTime(2026, 12, 31)
        });
        catalog.Setup(c => c.GetAvailabilityAsync(listingId, It.IsAny<DateOnly>()))
            .ReturnsAsync(new CatalogAvailabilityResponse
            {
                IsOperatingDay = true,
                Slots = new() { new CatalogAvailabilitySlot { TimeSlot = "09:00", RemainingCapacity = 10 } }
            });
        catalog.Setup(c => c.ReserveCapacityAsync(listingId, It.IsAny<DateOnly>(), "09:00", 2)).ReturnsAsync(true);
        var kafka = new Mock<IKafkaProducer>();
        var controller = new BookingsController(db, catalog.Object, kafka.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    { new Claim(ClaimTypes.NameIdentifier, visitorId.ToString()) }, "test"))
                }
            }
        };
        var result = await controller.CreateBooking(new CreateBookingRequest
        {
            ListingId = listingId, BookingDate = new DateOnly(2026, 11, 2),
            TimeSlot = "09:00", ParticipantCount = 2
        });
        if (identity is null || identity == Guid.Empty)
        {
            Assert.Equal(503, Assert.IsType<ObjectResult>(result).StatusCode);
            Assert.Empty(db.Bookings);
            catalog.Verify(c => c.ReserveCapacityAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(),
                It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            Assert.Empty(kafka.Invocations);
        }
        else
        {
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(identity, Assert.Single(db.Bookings).ProviderUserId);
            kafka.Verify(k => k.PublishAsync("booking.created", It.IsAny<string>(),
                It.Is<BookingCreatedEvent>(e => e.ProviderUserId == identity && e.VisitorId == visitorId),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
