using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProviderCatalogService.Data;
using ProviderCatalogService.Events;
using ProviderCatalogService.Models;
using ProviderCatalogService.Services;
using Shared.Kafka;
using Xunit;

namespace ProviderCatalogService.Tests;

public class BookingLifecycleConsumerTests
{
    [Theory]
    [InlineData("Experience", 3, "09:00 AM - 11:00 AM")]
    [InlineData("Accommodation", 1, "Stay (Min 2 Nights)")]
    [InlineData("Restaurant", 4, "06:00 PM - 08:00 PM")]
    public async Task CreatedNotifications_DoNotMutateCapacity_AndCancellationRestoresQuantity(
        string listingType, int quantity, string timeSlot)
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<AvailabilityService>();
        using var provider = services.BuildServiceProvider();
        var listingId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var total = listingType == "Accommodation" ? 1 : 10;

        // Inventory was already reserved synchronously before publication.
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            if (listingType == "Experience") db.ActivityListings.Add(new ActivityListing
            { Id = listingId, MaxParticipants = total, TimeSlots = JsonSerializer.Serialize(new[] { timeSlot }) });
            else if (listingType == "Restaurant") db.RestaurantListings.Add(new RestaurantListing
            { Id = listingId, SeatingCapacity = total, TimeSlots = timeSlot });
            else db.AccommodationListings.Add(new AccommodationListing { Id = listingId, MinStayNights = 2 });
            db.AvailabilitySlots.Add(new AvailabilitySlot
            {
                ListingId = listingId,
                ListingType = listingType,
                Date = date,
                TimeSlot = timeSlot,
                TotalCapacity = total,
                RemainingCapacity = total - quantity
            });
            await db.SaveChangesAsync();
        }

        var created = new TestCreatedConsumer();
        var payload = JsonSerializer.Serialize(new BookingCreatedEvent
        {
            BookingId = bookingId,
            VisitorId = Guid.NewGuid(),
            ListingId = listingId,
            ListingType = listingType,
            BookingDate = date.ToString("yyyy-MM-dd"),
            TimeSlot = timeSlot,
            ParticipantCount = quantity,
            TotalAmount = 1000m,
            CreatedAt = DateTime.UtcNow
        });
        await created.Handle(payload);
        await created.Handle(payload);

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            Assert.Equal(total - quantity, (await db.AvailabilitySlots.SingleAsync()).RemainingCapacity);
        }

        var canceled = new TestCanceledConsumer(provider.GetRequiredService<IServiceScopeFactory>());
        // Kafka is at-least-once: redelivery must not add capacity again.
        for (var delivery = 0; delivery < 2; delivery++)
        await canceled.Handle(JsonSerializer.Serialize(new BookingCanceledEvent
        {
            BookingId = bookingId,
            ListingId = listingId,
            ListingType = listingType,
            BookingDate = date.ToString("yyyy-MM-dd"),
            TimeSlot = timeSlot,
            ParticipantCount = quantity
        }));

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            Assert.Equal(total, (await db.AvailabilitySlots.SingleAsync()).RemainingCapacity);
            Assert.Single(db.BookingCapacityReleases);
            var availability = await scope.ServiceProvider.GetRequiredService<AvailabilityService>()
                .GetAvailabilityForDateAsync(listingId, date);
            Assert.NotNull(availability);
            Assert.Equal(total, Assert.Single(availability.Slots).RemainingCapacity);
        }
    }

    [Fact]
    public void EventContracts_DoNotDefaultToExperience()
    {
        Assert.Equal(string.Empty, new BookingCreatedEvent().ListingType);
        Assert.Equal(string.Empty, new BookingCanceledEvent().ListingType);
    }

    private sealed class TestCreatedConsumer : BookingCreatedConsumer
    {
        public TestCreatedConsumer()
            : base(Options.Create(new KafkaSettings()), NullLogger<BookingCreatedConsumer>.Instance) { }

        public Task Handle(string payload) =>
            HandleMessageAsync("booking.created", null, payload, CancellationToken.None);
    }

    private sealed class TestCanceledConsumer : BookingCanceledConsumer
    {
        public TestCanceledConsumer(IServiceScopeFactory scopeFactory)
            : base(Options.Create(new KafkaSettings()), NullLogger<BookingCanceledConsumer>.Instance, scopeFactory) { }

        public Task Handle(string payload) =>
            HandleMessageAsync("booking.canceled", null, payload, CancellationToken.None);
    }
}
