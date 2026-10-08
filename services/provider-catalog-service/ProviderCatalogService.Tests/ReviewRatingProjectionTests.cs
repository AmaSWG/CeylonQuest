using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProviderCatalogService.Controllers;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Events;
using ProviderCatalogService.Models;
using ProviderCatalogService.Services;
using Shared.Kafka;
using Xunit;

namespace ProviderCatalogService.Tests;

public class ReviewRatingProjectionTests
{
    private static CatalogDbContext Db() => new(new DbContextOptionsBuilder<CatalogDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static IListingRating Seed(CatalogDbContext db, string type)
    {
        var provider = new Provider { Id = Guid.NewGuid(), BusinessName = "Provider" }; db.Providers.Add(provider);
        var id = Guid.NewGuid();
        IListingRating listing = type switch
        {
            "Experience" => new ActivityListing { Id = id, ProviderId = provider.Id, Title = "Activity" },
            "Restaurant" => new RestaurantListing { Id = id, ProviderId = provider.Id, Name = "Restaurant" },
            _ => new AccommodationListing { Id = id, ProviderId = provider.Id, RoomType = "Room" }
        };
        db.Add(listing); db.SaveChanges(); return listing;
    }
    private static ReviewSubmittedEvent Event(IListingRating listing, string type, int rating = 5)
        => new(Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(), type, listing.Id, "Listing",
            Guid.NewGuid(), Guid.NewGuid(), rating, DateTime.UtcNow);

    [Theory]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public async Task Events_UpdateExactSumCountAndAverage_AndDuplicatesDoNotCount(string type)
    {
        using var db = Db(); var listing = Seed(db, type); var service = new ReviewRatingProjectionService(db);
        var first = Event(listing, type, 1); var second = Event(listing, type, 5);
        await service.ApplyAsync(first); await service.ApplyAsync(second);
        await service.ApplyAsync(first); await service.ApplyAsync(first with { EventId = Guid.NewGuid() });
        Assert.Equal(6, listing.RatingSum); Assert.Equal(2, listing.ReviewCount); Assert.Equal(3m, listing.AverageRating);
        Assert.Equal(2, db.ReviewRatingContributions.Count());
        await service.ApplyAsync(Event(listing, type, 4));
        Assert.Equal(10, listing.RatingSum); Assert.Equal(3, listing.ReviewCount); Assert.Equal(3.33m, listing.AverageRating);
    }

    [Theory]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public async Task Rebuild_RepairsTotalsFromContributions_AndReplayRemainsIdempotent(string type)
    {
        using var db = Db(); var listing = Seed(db, type); var service = new ReviewRatingProjectionService(db);
        var evt = Event(listing, type, 4); await service.ApplyAsync(evt);
        listing.RatingSum = 99; listing.ReviewCount = 25; listing.AverageRating = 3.96m; db.SaveChanges();
        await service.RebuildAsync(listing.Id, type); await service.ApplyAsync(evt);
        Assert.Equal(4, listing.RatingSum); Assert.Equal(1, listing.ReviewCount); Assert.Equal(4m, listing.AverageRating);
        Assert.Single(db.ReviewRatingContributions);
    }

    [Fact]
    public async Task EmptyRebuild_ResetsSummary_AndOtherListingsRemainUnchanged()
    {
        using var db = Db(); var a = Seed(db, "Experience"); var b = Seed(db, "Restaurant");
        var service = new ReviewRatingProjectionService(db); await service.ApplyAsync(Event(b, "Restaurant", 5));
        a.RatingSum = 10; a.ReviewCount = 2; a.AverageRating = 5; db.SaveChanges();
        await service.RebuildAsync(a.Id, "Experience");
        Assert.Equal(0, a.ReviewCount); Assert.Equal(0m, a.AverageRating);
        Assert.Equal(1, b.ReviewCount); Assert.Equal(5m, b.AverageRating);
    }

    [Fact]
    public async Task InvalidEvents_CreateNoContributionOrRatingUpdate()
    {
        using var db = Db(); var a = Seed(db, "Experience"); var valid = Event(a, "Experience");
        foreach (var evt in new[] { valid with { Rating = 0 }, valid with { Rating = 6 }, valid with { BookingType = "Platform" },
            valid with { EventVersion = 2 }, valid with { EventId = Guid.Empty }, valid with { ReviewId = Guid.Empty },
            valid with { ListingId = Guid.Empty }, valid with { ProviderId = Guid.Empty } })
            await Assert.ThrowsAsync<ArgumentException>(() => new ReviewRatingProjectionService(db).ApplyAsync(evt));
        Assert.Empty(db.ReviewRatingContributions); Assert.Equal(0, a.ReviewCount);
    }

    [Fact]
    public async Task DeletedListing_RetainsContribution_WithoutBlockingOtherEvents()
    {
        using var db = Db();
        var evt = Event(new ActivityListing { Id = Guid.NewGuid() }, "Experience");
        Assert.False(await new ReviewRatingProjectionService(db).ApplyAsync(evt));
        Assert.Single(await db.ReviewRatingContributions.AsNoTracking().ToListAsync());
        var next = Seed(db, "Restaurant"); await new ReviewRatingProjectionService(db).ApplyAsync(Event(next, "Restaurant"));
        Assert.Equal(1, next.ReviewCount);
    }

    private sealed class TestConsumer : ReviewSubmittedConsumer
    {
        public TestConsumer(IServiceScopeFactory scopes) : base(Options.Create(new KafkaSettings()),
            NullLogger<ReviewSubmittedConsumer>.Instance, scopes) { }
        public Task Handle(string payload) => HandleMessageAsync("review.submitted", null, payload, CancellationToken.None);
    }

    [Fact]
    public async Task Consumer_DeserializesContract_Deduplicates_AndIgnoresMalformedEvents()
    {
        var name = Guid.NewGuid().ToString();
        var services = new ServiceCollection().AddDbContext<CatalogDbContext>(o => o.UseInMemoryDatabase(name))
            .AddScoped<ReviewRatingProjectionService>();
        using var provider = services.BuildServiceProvider(); ReviewSubmittedEvent evt;
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>(); evt = Event(Seed(db, "Restaurant"), "Restaurant");
        }
        var consumer = new TestConsumer(provider.GetRequiredService<IServiceScopeFactory>());
        await consumer.Handle(JsonSerializer.Serialize(evt)); await consumer.Handle(JsonSerializer.Serialize(evt));
        await consumer.Handle("invalid-json"); await consumer.Handle("null");
        await consumer.Handle(JsonSerializer.Serialize(evt with { Rating = 6 }));
        using var check = provider.CreateScope(); var context = check.ServiceProvider.GetRequiredService<CatalogDbContext>();
        Assert.Single(context.ReviewRatingContributions); Assert.Equal(1, context.RestaurantListings.Single().ReviewCount);
    }

    [Fact]
    public async Task Consumer_PropagatesDatabaseFailures_InsteadOfAcknowledgingEvent()
    {
        using var provider = new ServiceCollection().AddScoped<CatalogDbContext>(_ => new FailingDb())
            .AddScoped<ReviewRatingProjectionService>().BuildServiceProvider();
        var consumer = new TestConsumer(provider.GetRequiredService<IServiceScopeFactory>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Handle(JsonSerializer.Serialize(
            Event(new ActivityListing { Id = Guid.NewGuid() }, "Experience"))));
    }

    private sealed class FailingDb : CatalogDbContext
    {
        public FailingDb() : base(new DbContextOptionsBuilder<CatalogDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options) { }
        public override Task<int> SaveChangesAsync(CancellationToken token = default)
            => throw new InvalidOperationException("Database unavailable");
    }

    [Fact]
    public async Task ExactHalfAverage_RoundsConsistentlyWithBookingSummary()
    {
        using var db = Db(); var listing = Seed(db, "Experience"); var service = new ReviewRatingProjectionService(db);
        for (var i = 0; i < 8; i++) await service.ApplyAsync(Event(listing, "Experience", i == 0 ? 2 : 1));
        Assert.Equal(9, listing.RatingSum); Assert.Equal(8, listing.ReviewCount); Assert.Equal(1.13m, listing.AverageRating);
    }

    [Fact]
    public async Task PublicListingAndSearchResponses_ExposeProjectedRatingsForAllTypes()
    {
        using var db = Db(); var service = new ReviewRatingProjectionService(db);
        var a = Seed(db, "Experience"); var r = Seed(db, "Restaurant"); var h = Seed(db, "Accommodation");
        await service.ApplyAsync(Event(a, "Experience", 4)); await service.ApplyAsync(Event(r, "Restaurant", 3));
        await service.ApplyAsync(Event(h, "Accommodation", 5));
        var activity = Assert.IsType<ActivityListingResponse>(Assert.IsType<OkObjectResult>(
            await new ActivityListingsController(db).GetPublicListingById(a.Id)).Value);
        Assert.Equal(4m, activity.AverageRating); Assert.Equal(1, activity.ReviewCount);
        var restaurants = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<RestaurantListingResponse>>(
            Assert.IsType<OkObjectResult>(await new RestaurantListingsController(db).GetPublicListings(null, null, null)).Value);
        Assert.Equal(3m, Assert.Single(restaurants).AverageRating);
        var accommodations = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<AccommodationListingResponse>>(
            Assert.IsType<OkObjectResult>(await new AccommodationListingsController(db).GetPublicListings(null, null, null, null)).Value);
        Assert.Equal(5m, Assert.Single(accommodations).AverageRating);
        var search = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(Assert.IsType<OkObjectResult>(
            await new SearchController(db).Search()).Value);
        Assert.Equal(3, search.Items.Count());
        Assert.All(search.Items, item => Assert.Equal(1, item.ReviewCount));
        Assert.Equal(4m, search.Items.Single(i => i.Id == a.Id).AverageRating);
        Assert.Equal(3m, search.Items.Single(i => i.Id == r.Id).AverageRating);
        Assert.Equal(5m, search.Items.Single(i => i.Id == h.Id).AverageRating);
    }

    [Fact]
    public void MySqlModel_UsesUniqueInboxAndMappedRatingFields()
    {
        using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseMySql("Server=localhost;Database=catalog;User=root;", new MySqlServerVersion(new Version(8, 0, 30))).Options);
        var model = db.Model.FindEntityType(typeof(ReviewRatingContribution));
        Assert.Equal("ReviewId", Assert.Single(model.FindPrimaryKey().Properties).Name);
        Assert.Contains(model.GetIndexes(), i => i.IsUnique && i.Properties.Single().Name == "EventId");
        Assert.Contains("RatingSum", db.ActivityListings.Where(l => l.Id == Guid.NewGuid()).ToQueryString());
        Assert.Contains("ROUND(", RatingSql<ActivityListing>(db));
        Assert.Contains("ROUND(", RatingSql<RestaurantListing>(db));
        Assert.Contains("ROUND(", RatingSql<AccommodationListing>(db));
        var auth = Assert.Single(typeof(ReviewRatingsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal("Admin", auth.Roles);
    }

    private static string RatingSql<T>(CatalogDbContext db) where T : class, IListingRating
        => db.Set<T>().Where(l => l.Id == Guid.NewGuid())
            .Select(l => Math.Round(l.RatingSum / (decimal)l.ReviewCount, 2)).ToQueryString();
}
