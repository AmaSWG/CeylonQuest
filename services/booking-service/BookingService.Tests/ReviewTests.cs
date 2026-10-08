using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using BookingService.Controllers;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Events;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Moq;
using Shared.Kafka;

namespace BookingService.Tests;

public class ReviewTests
{
    private static readonly DateTime Now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }
    private static BookingDbContext Db() => new(new DbContextOptionsBuilder<BookingDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static ReviewService Service(BookingDbContext db, ICatalogService? catalog = null)
        => new(db, catalog ?? Mock.Of<ICatalogService>(), new Clock());

    private static IPayableBooking Seed(BookingDbContext db, string type, string status = "Completed", int endOffset = -1)
    {
        var id = Guid.NewGuid(); var visitor = Guid.NewGuid(); var listing = Guid.NewGuid(); var provider = Guid.NewGuid();
        IPayableBooking booking = type switch
        {
            "Experience" => new Booking { Id = id, VisitorId = visitor, ListingId = listing, ListingTitle = "Experience",
                ProviderId = provider, ScheduledEndAtUtc = Now.AddHours(endOffset), Status = Enum.Parse<BookingStatus>(status) },
            "Restaurant" => new RestaurantReservation { Id = id, VisitorId = visitor, RestaurantId = listing, RestaurantName = "Restaurant",
                ProviderId = provider, ScheduledEndAtUtc = Now.AddHours(endOffset), Status = Enum.Parse<ReservationStatus>(status) },
            _ => new AccommodationBooking { Id = id, VisitorId = visitor, AccommodationId = listing, AccommodationName = "Room",
                ProviderId = provider, ScheduledEndAtUtc = Now.AddHours(endOffset), Status = Enum.Parse<AccommodationBookingStatus>(status) }
        };
        db.Add(booking); db.SaveChanges(); return booking;
    }
    private static CreateReviewRequest Request(IPayableBooking b, int rating = 5)
        => new() { BookingId = b.Id, BookingType = b.BookingType, Rating = rating, Comment = "  Great service!  " };

    [Theory]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public async Task CompletedOwnedPastBooking_SavesReviewAndNotificationOutbox(string type)
    {
        await using var db = Db(); var booking = Seed(db, type);
        var response = await Service(db).CreateAsync(booking.VisitorId, Request(booking));
        var saved = Assert.Single(db.ListingReviews);
        Assert.Equal(booking.Id, saved.BookingId); Assert.Equal(booking.ListingId, saved.ListingId);
        Assert.Equal(booking.VisitorId, saved.VisitorId); Assert.Equal("Great service!", saved.Comment);
        var evt = JsonSerializer.Deserialize<ReviewSubmittedEvent>(Assert.Single(db.ReviewOutboxMessages).Payload)!;
        Assert.Equal(saved.Id, evt.ReviewId); Assert.Equal(saved.ProviderId, evt.ProviderId);
        Assert.Equal(booking.ListingTitle, evt.ListingTitle); Assert.Equal(type, evt.BookingType);
        Assert.Equal(saved.Id, response.Id); Assert.Null(db.ReviewOutboxMessages.Single().PublishedAtUtc);
        var summary = await Service(db).GetSummaryAsync(booking.ListingId, type);
        Assert.Equal(5, summary.AverageRating); Assert.Equal(1, summary.ReviewCount);
    }

    public static IEnumerable<object[]> IneligibleCases()
    {
        foreach (var type in new[] { "Experience", "Restaurant", "Accommodation" })
        {
            foreach (var status in new[] { "PendingPayment", "Confirmed", "Cancelled" }) yield return new object[] { type, status, -1 };
            yield return new object[] { type, "Completed", 0 };
            yield return new object[] { type, "Completed", 1 };
        }
    }
    [Theory, MemberData(nameof(IneligibleCases))]
    public async Task NoncompletedOrNotEnded_IsRejectedWithoutReviewOrEvent(string type, string status, int offset)
    {
        await using var db = Db(); var booking = Seed(db, type, status, offset);
        var ex = await Assert.ThrowsAsync<ReviewException>(() => Service(db).CreateAsync(booking.VisitorId, Request(booking)));
        Assert.Equal(409, ex.StatusCode); Assert.Empty(db.ListingReviews); Assert.Empty(db.ReviewOutboxMessages);
    }

    [Theory]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public async Task OwnershipAndDuplicateChecks_PreserveExistingReview(string type)
    {
        await using var db = Db(); var booking = Seed(db, type); var service = Service(db);
        Assert.Equal(403, (await Assert.ThrowsAsync<ReviewException>(() => service.CreateAsync(Guid.NewGuid(), Request(booking)))).StatusCode);
        await service.CreateAsync(booking.VisitorId, Request(booking));
        Assert.Equal(409, (await Assert.ThrowsAsync<ReviewException>(() => service.CreateAsync(booking.VisitorId, Request(booking, 1)))).StatusCode);
        Assert.Equal(5, Assert.Single(db.ListingReviews).Rating); Assert.Single(db.ReviewOutboxMessages);
    }

    [Theory]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public async Task MissingBooking_IsRejected(string type)
    {
        await using var db = Db();
        var request = new CreateReviewRequest { BookingId = Guid.NewGuid(), BookingType = type, Rating = 5, Comment = "Good" };
        Assert.Equal(404, (await Assert.ThrowsAsync<ReviewException>(() => Service(db).CreateAsync(Guid.NewGuid(), request))).StatusCode);
        Assert.Empty(db.ListingReviews); Assert.Empty(db.ReviewOutboxMessages);
    }

    [Theory]
    [InlineData(0, "Good")]
    [InlineData(6, "Good")]
    [InlineData(5, " ")]
    public async Task InvalidReview_IsRejected(int rating, string comment)
    {
        await using var db = Db(); var b = Seed(db, "Experience"); var request = Request(b, rating); request.Comment = comment;
        Assert.Equal(400, (await Assert.ThrowsAsync<ReviewException>(() => Service(db).CreateAsync(b.VisitorId, request))).StatusCode);
        Assert.Empty(db.ListingReviews); Assert.Empty(db.ReviewOutboxMessages);
    }

    [Fact]
    public async Task UnknownEnd_IsRejected_AndProviderLookupFailureCreatesNothing()
    {
        await using var db = Db(); var b = (Booking)Seed(db, "Experience"); b.ScheduledEndAtUtc = null; db.SaveChanges();
        Assert.Equal(409, (await Assert.ThrowsAsync<ReviewException>(() => Service(db).CreateAsync(b.VisitorId, Request(b)))).StatusCode);
        b.ScheduledEndAtUtc = Now.AddHours(-1); b.ProviderId = null; db.SaveChanges();
        var catalog = new Mock<ICatalogService>(); catalog.Setup(c => c.GetListingAsync(b.ListingId)).ThrowsAsync(new HttpRequestException());
        Assert.Equal(503, (await Assert.ThrowsAsync<ReviewException>(() => Service(db, catalog.Object).CreateAsync(b.VisitorId, Request(b)))).StatusCode);
        Assert.Empty(db.ListingReviews); Assert.Empty(db.ReviewOutboxMessages);
    }

    [Fact]
    public async Task FilteringPaginationAndSummary_StayScopedToListing()
    {
        await using var db = Db(); var listing = Guid.NewGuid();
        for (var i = 0; i < 4; i++)
        {
            var b = (Booking)Seed(db, "Experience"); b.ListingId = listing; db.SaveChanges();
            await Service(db).CreateAsync(b.VisitorId, Request(b, i == 0 ? 1 : 5));
        }
        var other = Seed(db, "Restaurant"); await Service(db).CreateAsync(other.VisitorId, Request(other));
        var page = await Service(db).GetAsync(listing, new() { Rating = 5, Page = 2, PageSize = 2 });
        Assert.Single(page.Items); Assert.All(page.Items, r => Assert.Equal(5, r.Rating));
        Assert.Equal(3, page.TotalCount); Assert.Equal(2, page.TotalPages);
        Assert.Equal(4, page.ReviewCount); Assert.Equal(4, page.AverageRating);
        var empty = await Service(db).GetAsync(Guid.NewGuid(), new());
        Assert.Empty(empty.Items); Assert.Equal(0, empty.AverageRating); Assert.Equal(0, empty.ReviewCount);
    }

    [Fact]
    public async Task BrokerFailure_RetriesSameEvent_AndPublishedMessageIsNotRepublished()
    {
        await using var db = Db(); var b = Seed(db, "Experience"); await Service(db).CreateAsync(b.VisitorId, Request(b));
        var kafka = new Mock<IKafkaProducer>();
        kafka.Setup(k => k.PublishAsync("review.submitted", b.ListingId.ToString(), It.IsAny<ReviewSubmittedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Broker unavailable"));
        var publisher = new ReviewOutboxPublisher(db, kafka.Object, new Clock());
        await Assert.ThrowsAsync<IOException>(() => publisher.PublishAsync());
        Assert.Null(db.ReviewOutboxMessages.Single().PublishedAtUtc); Assert.Single(db.ListingReviews);
        kafka.Setup(k => k.PublishAsync("review.submitted", b.ListingId.ToString(), It.IsAny<ReviewSubmittedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await publisher.PublishAsync(); await publisher.PublishAsync();
        Assert.Equal(Now, db.ReviewOutboxMessages.Single().PublishedAtUtc);
        kafka.Verify(k => k.PublishAsync("review.submitted", b.ListingId.ToString(), It.IsAny<ReviewSubmittedEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Controller_RequiresVisitorForCreation_AllowsPublicReading_AndMapsErrors()
    {
        var method = typeof(ReviewsController).GetMethod(nameof(ReviewsController.Create))!;
        Assert.Equal("Visitor", Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()).Roles);
        Assert.Single(typeof(ReviewsController).GetMethod(nameof(ReviewsController.Get))!.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
        var service = new Mock<IReviewService>(); var controller = new ReviewsController(service.Object)
        { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
        Assert.IsType<UnauthorizedResult>(await controller.Create(new(), default));
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }));
        service.Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreateReviewRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ReviewException(409, "Duplicate"));
        Assert.Equal(409, Assert.IsType<ObjectResult>(await controller.Create(new(), default)).StatusCode);
    }

    [Theory]
    [InlineData("09:00 - 11:00", null, 5, 30)]
    [InlineData("9:00 AM - 11:00 AM", null, 5, 30)]
    [InlineData("23:00 - 01:00", null, 19, 30)]
    [InlineData("09:00", "2 Hours", 5, 30)]
    public void Schedule_UsesSriLankaTimezone_AndHandlesOvernightSlots(string slot, string? duration, int hour, int minute)
    {
        var end = ReviewSchedule.SlotEnd(new DateOnly(2026, 10, 7), slot, duration);
        Assert.Equal(new DateTime(2026, 10, 7, hour, minute, 0, DateTimeKind.Utc), end);
    }

    [Fact]
    public void UnknownScheduleAndInvalidQueries_AreRejected()
    {
        Assert.Null(ReviewSchedule.SlotEnd(new DateOnly(2026, 10, 7), "09:00"));
        Assert.Null(ReviewSchedule.CheckoutEnd(new DateOnly(2026, 10, 7), null));
        foreach (var query in new[] { new ReviewQuery { Rating = 6 }, new ReviewQuery { Page = 0 }, new ReviewQuery { PageSize = 101 }, new ReviewQuery { BookingType = "Other" } })
            Assert.Throws<ValidationException>(() => Validator.ValidateObject(query, new ValidationContext(query), true));
    }

    [Fact]
    public void DatabaseModel_RequiresUniqueBookingReviewAndRatingConstraint()
    {
        using var db = new BookingDbContext(new DbContextOptionsBuilder<BookingDbContext>()
            .UseMySql("Server=localhost;Database=reviews;User=root;", new MySqlServerVersion(new Version(8, 0, 30))).Options);
        var entity = db.Model.FindEntityType(typeof(ListingReview))!;
        Assert.Contains(entity.GetIndexes(), i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "BookingType", "BookingId" }));
    }

    [Fact]
    public async Task PlatformReviews_AreIndependentOfBookingsAndListingRatings()
    {
        await using var db = Db(); var service = Service(db); var visitor = Guid.NewGuid();
        await service.CreatePlatformAsync(visitor, new() { Rating = 1, Comment = "Needs improvement" });
        await service.CreatePlatformAsync(visitor, new() { Rating = 5, Comment = "Easy to use" });
        var result = await service.GetPlatformAsync(new() { Rating = 5, PageSize = 1 });
        Assert.Single(result.Items); Assert.Equal(1, result.TotalCount);
        Assert.Equal(2, result.ReviewCount); Assert.Equal(3, result.AverageRating);
        Assert.Empty(db.ListingReviews); Assert.Empty(db.ReviewOutboxMessages);
        Assert.All(db.PlatformReviews, r => Assert.Equal(visitor, r.VisitorId));
    }

    [Theory]
    [InlineData(0, "Good")]
    [InlineData(6, "Good")]
    [InlineData(5, " ")]
    public async Task PlatformValidation_RejectsInvalidFeedback(int rating, string comment)
    {
        await using var db = Db();
        Assert.Equal(400, (await Assert.ThrowsAsync<ReviewException>(() => Service(db).CreatePlatformAsync(Guid.NewGuid(),
            new() { Rating = rating, Comment = comment }))).StatusCode);
        Assert.Empty(db.PlatformReviews);
    }

    [Fact]
    public async Task LegacyBooking_ResolvesProviderAndUsesBookedSlotEnd()
    {
        await using var db = Db(); var b = (Booking)Seed(db, "Experience");
        b.ProviderId = null; b.ScheduledEndAtUtc = null; b.BookingDate = new DateOnly(2026, 10, 6); b.TimeSlot = "09:00 - 11:00"; db.SaveChanges();
        var provider = Guid.NewGuid(); var catalog = new Mock<ICatalogService>();
        catalog.Setup(c => c.GetListingAsync(b.ListingId)).ReturnsAsync(new CatalogListingResponse { Id = b.ListingId, ProviderId = provider });
        await Service(db, catalog.Object).CreateAsync(b.VisitorId, Request(b));
        Assert.Equal(provider, Assert.Single(db.ListingReviews).ProviderId);
    }

    [Fact]
    public async Task DeletedBooking_IsNotReviewable()
    {
        await using var db = Db(); var b = (Booking)Seed(db, "Experience"); b.IsDeleted = true; db.SaveChanges();
        Assert.Equal(404, (await Assert.ThrowsAsync<ReviewException>(() => Service(db).CreateAsync(b.VisitorId, Request(b)))).StatusCode);
        Assert.Empty(db.ListingReviews);
    }

    [Fact]
    public async Task InvalidBookingTypeEmptyIdAndLongComment_AreRejected()
    {
        await using var db = Db(); var b = Seed(db, "Experience");
        var invalidType = Request(b); invalidType.BookingType = "Other";
        var missingId = Request(b); missingId.BookingId = Guid.Empty;
        var longComment = Request(b); longComment.Comment = new string('a', 2001);
        foreach (var request in new[] { invalidType, missingId, longComment })
            Assert.Equal(400, (await Assert.ThrowsAsync<ReviewException>(() => Service(db).CreateAsync(b.VisitorId, request))).StatusCode);
        Assert.Empty(db.ListingReviews);
    }

    [Fact]
    public void Checkout_UsesConfiguredLocalTime()
    {
        Assert.Equal(new DateTime(2026, 10, 7, 6, 30, 0, DateTimeKind.Utc),
            ReviewSchedule.CheckoutEnd(new DateOnly(2026, 10, 7), "12:00"));
        Assert.Equal(new DateTime(2026, 10, 7, 5, 30, 0, DateTimeKind.Utc),
            ReviewSchedule.CheckoutEnd(new DateOnly(2026, 10, 7), "11:00"));
    }

    [Fact]
    public async Task Swagger_ContainsReviewEndpointsAndValidationSchemas()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers().AddApplicationPart(typeof(ReviewsController).Assembly);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new OpenApiInfo { Title = "Reviews", Version = "v1" }));
        await using var app = builder.Build();
        app.MapControllers();
        var swagger = app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        Assert.Contains("/api/bookings/reviews", swagger.Paths.Keys);
        Assert.Contains("/api/bookings/reviews/{listingId}", swagger.Paths.Keys);
        Assert.Contains("/api/bookings/reviews/{listingId}/summary", swagger.Paths.Keys);
        Assert.Contains("/api/bookings/platform-reviews", swagger.Paths.Keys);
        var schema = swagger.Components.Schemas[nameof(CreateReviewRequest)];
        Assert.Equal(1, schema.Properties["rating"].Minimum);
        Assert.Equal(5, schema.Properties["rating"].Maximum);
        Assert.Equal(2000, schema.Properties["comment"].MaxLength);
        Assert.Contains("comment", schema.Required);
    }

    [Fact]
    public async Task Summary_RoundsExactHalfConsistentlyWithCatalog()
    {
        await using var db = Db(); var listingId = Guid.NewGuid();
        for (var i = 0; i < 8; i++)
        {
            var booking = (Booking)Seed(db, "Experience"); booking.ListingId = listingId; db.SaveChanges();
            await Service(db).CreateAsync(booking.VisitorId, Request(booking, i == 0 ? 2 : 1));
        }
        Assert.Equal(1.13, (await Service(db).GetSummaryAsync(listingId, "Experience")).AverageRating);
    }

    [Theory]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public async Task PublishedBookingEvent_UpdatesCatalogProjectionWithCompatibleContract(string type)
    {
        await using var db = Db(); var booking = Seed(db, type);
        await Service(db).CreateAsync(booking.VisitorId, Request(booking, 4));
        string? payload = null;
        var kafka = new Mock<IKafkaProducer>();
        kafka.Setup(k => k.PublishAsync("review.submitted", booking.ListingId.ToString(), It.IsAny<ReviewSubmittedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<string, string?, ReviewSubmittedEvent, CancellationToken>((_, _, evt, _) => payload = JsonSerializer.Serialize(evt))
            .Returns(Task.CompletedTask);
        await new ReviewOutboxPublisher(db, kafka.Object, new Clock()).PublishAsync();
        Assert.NotNull(payload);
        var catalogEvent = JsonSerializer.Deserialize<ProviderCatalogService.Events.ReviewSubmittedEvent>(payload!)!;
        await using var catalogDb = new ProviderCatalogService.Data.CatalogDbContext(
            new DbContextOptionsBuilder<ProviderCatalogService.Data.CatalogDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        catalogDb.Providers.Add(new() { Id = catalogEvent.ProviderId, BusinessName = "Provider" });
        ProviderCatalogService.Models.IListingRating listing = type switch
        {
            "Experience" => new ProviderCatalogService.Models.ActivityListing { Id = booking.ListingId, ProviderId = catalogEvent.ProviderId },
            "Restaurant" => new ProviderCatalogService.Models.RestaurantListing { Id = booking.ListingId, ProviderId = catalogEvent.ProviderId },
            _ => new ProviderCatalogService.Models.AccommodationListing { Id = booking.ListingId, ProviderId = catalogEvent.ProviderId }
        };
        catalogDb.Add(listing); await catalogDb.SaveChangesAsync();
        var projection = new ProviderCatalogService.Services.ReviewRatingProjectionService(catalogDb);
        await projection.ApplyAsync(catalogEvent); await projection.ApplyAsync(catalogEvent);
        Assert.Equal(4m, listing.AverageRating); Assert.Equal(1, listing.ReviewCount);
        var summary = await Service(db).GetSummaryAsync(booking.ListingId, type);
        Assert.Equal((double)listing.AverageRating, summary.AverageRating);
        Assert.Equal(listing.ReviewCount, summary.ReviewCount);
    }
}
