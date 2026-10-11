using System.Text.Json;
using BookingService.Data;
using BookingService.Events;
using BookingService.Models;
using BookingService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ProviderCatalogService.Data;
using ProviderCatalogService.Models;
using ProviderCatalogService.Services;
using Shared.Kafka;

namespace BookingService.Tests;

public class ExpirationCapacityFlowTests
{
    [Theory]
    [InlineData("Experience", 3, "09:00 AM - 11:00 AM")]
    [InlineData("Restaurant", 2, "06:00 PM - 07:00 PM")]
    [InlineData("Accommodation", 1, "Stay (Min 2 Nights)")]
    public async Task ExpirationOutboxAndConsumerRestorePersistedInventoryDespiteRetry(
        string type, int quantity, string slot)
    {
        var now = DateTime.UtcNow;
        var date = DateOnly.FromDateTime(now.AddDays(2));
        var listingId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var visitorId = Guid.NewGuid();
        var providerUserId = Guid.NewGuid();
        var total = type == "Accommodation" ? 1 : 10;
        using var bookingDb = new BookingDbContext(new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        IPayableBooking booking = type switch
        {
            "Restaurant" => new RestaurantReservation { Id = bookingId, VisitorId = visitorId, ProviderUserId = providerUserId, RestaurantId = listingId,
                ReservationDate = date, TimeSlot = slot, PartySize = quantity },
            "Accommodation" => new AccommodationBooking { Id = bookingId, VisitorId = visitorId, ProviderUserId = providerUserId, AccommodationId = listingId,
                CheckInDate = date, CheckOutDate = date.AddDays(2), GuestCount = 4 },
            _ => new Booking { Id = bookingId, VisitorId = visitorId, ProviderUserId = providerUserId, ListingId = listingId, BookingDate = date,
                TimeSlot = slot, ParticipantCount = quantity }
        };
        booking.CreatedAt = now.AddMinutes(-15);
        booking.PaymentStatus = PaymentStatus.Failed;
        bookingDb.Add(booking);
        await bookingDb.SaveChangesAsync();

        var catalogName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase(catalogName));
        services.AddScoped<AvailabilityService>();
        using var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            if (type == "Experience") db.ActivityListings.Add(new ActivityListing
            { Id = listingId, MaxParticipants = total, TimeSlots = JsonSerializer.Serialize(new[] { slot }) });
            else if (type == "Restaurant") db.RestaurantListings.Add(new RestaurantListing
            { Id = listingId, SeatingCapacity = total, TimeSlots = slot });
            else db.AccommodationListings.Add(new AccommodationListing { Id = listingId, MinStayNights = 2 });
            db.AvailabilitySlots.AddRange(
                new AvailabilitySlot { ListingId = listingId, ListingType = type, Date = date,
                    TimeSlot = slot, TotalCapacity = total, RemainingCapacity = total - quantity },
                new AvailabilitySlot { ListingId = listingId, ListingType = type, Date = date.AddDays(1),
                    TimeSlot = slot, TotalCapacity = total, RemainingCapacity = 0 });
            await db.SaveChangesAsync();
        }

        var consumer = new CancellationConsumer(provider.GetRequiredService<IServiceScopeFactory>());
        var kafka = new Mock<IKafkaProducer>();
        var attempts = 0;
        kafka.Setup(k => k.PublishAsync("booking.canceled", bookingId.ToString(),
                It.IsAny<BookingCanceledEvent>(), It.IsAny<CancellationToken>()))
            .Returns(async (string topic, string key, BookingCanceledEvent evt, CancellationToken token) =>
            {
                Assert.Equal(BookingStatus.Cancelled, booking.Status);
                Assert.Single(bookingDb.BookingCancellationMessages);
                Assert.Equal(visitorId, evt.VisitorId);
                Assert.Equal(providerUserId, evt.ProviderUserId);
                if (++attempts == 1) throw new InvalidOperationException("Broker unavailable");
                await consumer.Deliver(JsonSerializer.Serialize(evt));
                // Simulate delivery followed by loss of the producer acknowledgement.
                if (attempts == 2) throw new InvalidOperationException("Acknowledgement lost");
            });
        var expiration = new PendingPaymentExpirationService(bookingDb, kafka.Object);
        Assert.Equal(1, await expiration.ExpireAsync(now));
        await Assert.ThrowsAsync<InvalidOperationException>(() => expiration.PublishPendingCancellationsAsync());
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => expiration.PublishPendingCancellationsAsync());

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var availability = await scope.ServiceProvider.GetRequiredService<AvailabilityService>()
                .GetAvailabilityForDateAsync(listingId, date);
            Assert.Equal(total, Assert.Single(availability!.Slots).RemainingCapacity);
            Assert.Equal(0, (await db.AvailabilitySlots.SingleAsync(s => s.Date == date.AddDays(1))).RemainingCapacity);
            // Another visitor uses the released inventory before Kafka redelivery.
            (await db.AvailabilitySlots.SingleAsync(s => s.Date == date)).RemainingCapacity -= quantity;
            await db.SaveChangesAsync();
        }

        await expiration.PublishPendingCancellationsAsync();
        Assert.Equal(0, await expiration.ExpireAsync(now.AddMinutes(1)));
        await expiration.PublishPendingCancellationsAsync();
        Assert.NotNull((await bookingDb.BookingCancellationMessages.SingleAsync()).PublishedAt);
        Assert.Equal(3, attempts);
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            Assert.Single(db.BookingCapacityReleases);
            Assert.Equal(total - quantity, (await db.AvailabilitySlots.SingleAsync(s => s.Date == date)).RemainingCapacity);
        }
    }

    private sealed class CancellationConsumer(IServiceScopeFactory scopes)
        : BookingCanceledConsumer(Options.Create(new KafkaSettings()), NullLogger<BookingCanceledConsumer>.Instance, scopes)
    {
        public Task Deliver(string payload) => HandleMessageAsync("booking.canceled", null, payload, CancellationToken.None);
    }
}
