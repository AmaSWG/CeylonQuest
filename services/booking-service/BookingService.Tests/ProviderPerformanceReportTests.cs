using System.ComponentModel.DataAnnotations;
using BookingService.Controllers;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BookingService.Tests;

public class ProviderPerformanceReportTests
{
    private static readonly DateOnly Day = new(2026, 10, 10);

    private static BookingDbContext CreateDb() => new(
        new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Mock<ICatalogService> Catalog(
        params CatalogAdminListingResponse[] listings)
    {
        var mock = new Mock<ICatalogService>(MockBehavior.Strict);

        mock.Setup(c => c.GetAdminListingsAsync("admin-token"))
            .ReturnsAsync(listings.ToList());

        var providers = listings
            .GroupBy(l => l.ProviderId)
            .Select(g => new CatalogReportProviderResponse
            {
                ProviderId = g.Key,
                ProviderName = g.First().ProviderName,
                ServiceType = g.First().BookingType
            })
            .ToList();

        mock.Setup(c => c.GetReportProvidersAsync(
                "admin-token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        return mock;
    }

    private static CatalogAdminListingResponse Listing(
        Guid providerId, Guid listingId, string category) => new()
        {
            Id = listingId,
            ProviderId = providerId,
            ProviderName = providerId.ToString(),
            BookingType = category
        };

    private static ListingReview Review(
        Guid providerId, Guid listingId, string category,
        int rating, DateOnly? date = null) => new()
        {
            Id = Guid.NewGuid(),
            BookingId = Guid.NewGuid(),
            ProviderId = providerId,
            ListingId = listingId,
            BookingType = category,
            Rating = rating,
            CreatedAtUtc = (date ?? Day).ToDateTime(
            TimeOnly.MinValue, DateTimeKind.Utc)
        };

    [Fact]
    public async Task AggregatesRevenueAndEveryStarWithoutMultiplyingRevenue()
    {
        using var db = CreateDb();
        var provider = Guid.NewGuid();
        var experience = Guid.NewGuid();
        var restaurant = Guid.NewGuid();
        var accommodation = Guid.NewGuid();

        db.Bookings.AddRange(
            new Booking
            {
                ListingId = experience,
                BookingDate = Day,
                Status = BookingStatus.Completed,
                TotalAmount = 100,
                RefundAmount = 10
            },
            new Booking
            {
                ListingId = experience,
                BookingDate = Day,
                Status = BookingStatus.Cancelled,
                TotalAmount = 500
            },
            new Booking
            {
                ListingId = experience,
                BookingDate = Day,
                Status = BookingStatus.PendingPayment,
                TotalAmount = 500
            },
            new Booking
            {
                ListingId = experience,
                BookingDate = Day,
                Status = BookingStatus.Confirmed,
                TotalAmount = 500,
                IsDeleted = true
            });

        db.RestaurantReservations.Add(new RestaurantReservation
        {
            RestaurantId = restaurant,
            ReservationDate = Day,
            Status = ReservationStatus.Completed,
            TotalPrice = 200,
            RefundAmount = 20
        });

        db.AccommodationBookings.Add(new AccommodationBooking
        {
            AccommodationId = accommodation,
            CheckInDate = Day,
            CheckOutDate = Day.AddDays(1),
            Status = AccommodationBookingStatus.Confirmed,
            TotalPrice = 300,
            RefundAmount = 30
        });

        for (var rating = 1; rating <= 5; rating++)
            db.ListingReviews.Add(
                Review(provider, experience, "Experience", rating));

        await db.SaveChangesAsync();

        var service = new ProviderPerformanceReportService(
            db,
            Catalog(
                Listing(provider, experience, "Experience"),
                Listing(provider, restaurant, "Restaurant"),
                Listing(provider, accommodation, "Accommodation")).Object);

        var report = await service.GenerateAsync(
            "admin-token", new() { StartDate = Day, EndDate = Day });

        var row = Assert.Single(report.Providers);

        Assert.Equal(provider, row.ProviderId);
        Assert.Equal(540m, row.TotalRevenue);
        Assert.Equal(5, row.TotalReviewCount);
        Assert.Equal(3d, row.AverageRating!.Value);
        Assert.Equal(1, row.StarRatingBreakdown.OneStar);
        Assert.Equal(1, row.StarRatingBreakdown.TwoStars);
        Assert.Equal(1, row.StarRatingBreakdown.ThreeStars);
        Assert.Equal(1, row.StarRatingBreakdown.FourStars);
        Assert.Equal(1, row.StarRatingBreakdown.FiveStars);
        Assert.False(row.LowRating);
    }

    [Fact]
    public async Task ReturnsUnratedProvidersAndMarksLowRatings()
    {
        using var db = CreateDb();
        var low = Guid.NewGuid();
        var unrated = Guid.NewGuid();
        var listing = Guid.NewGuid();

        db.ListingReviews.Add(
            Review(low, listing, "Experience", 2));
        await db.SaveChangesAsync();

        var service = new ProviderPerformanceReportService(
            db,
            Catalog(
                Listing(low, listing, "Experience"),
                Listing(unrated, Guid.NewGuid(), "Experience")).Object);

        var report = await service.GenerateAsync("admin-token", new());

        Assert.True(report.Providers.Single(p => p.ProviderId == low).LowRating);

        var empty = report.Providers.Single(p => p.ProviderId == unrated);
        Assert.Equal(0, empty.TotalReviewCount);
        Assert.Equal(0m, empty.TotalRevenue);
        Assert.Null(empty.AverageRating);
        Assert.True(empty.NoReviews);
        Assert.False(empty.LowRating);
        Assert.Equal("No Reviews", empty.RatingState);
    }

    [Fact]
    public async Task AppliesDateCategoryProviderAndMinimumRatingTogether()
    {
        using var db = CreateDb();
        var provider = Guid.NewGuid();
        var otherProvider = Guid.NewGuid();
        var experience = Guid.NewGuid();
        var restaurant = Guid.NewGuid();

        db.Bookings.AddRange(
            new Booking
            {
                ListingId = experience,
                BookingDate = Day,
                Status = BookingStatus.Confirmed,
                TotalAmount = 100
            },
            new Booking
            {
                ListingId = experience,
                BookingDate = Day.AddDays(-1),
                Status = BookingStatus.Confirmed,
                TotalAmount = 900
            });

        db.ListingReviews.AddRange(
            Review(provider, experience, "Experience", 4),
            Review(provider, experience, "Experience", 1, Day.AddDays(-1)),
            Review(provider, experience, "Experience", 1, Day.AddDays(1)),
            Review(provider, restaurant, "Restaurant", 1));

        await db.SaveChangesAsync();

        var service = new ProviderPerformanceReportService(
            db,
            Catalog(
                Listing(provider, experience, "Experience"),
                Listing(provider, restaurant, "Restaurant"),
                Listing(otherProvider, Guid.NewGuid(), "Experience")).Object);

        var query = new ProviderPerformanceReportQuery
        {
            StartDate = Day,
            EndDate = Day,
            Category = "experience",
            ProviderId = provider,
            MinimumRating = 4
        };

        var row = Assert.Single(
            (await service.GenerateAsync("admin-token", query)).Providers);

        Assert.Equal(provider, row.ProviderId);
        Assert.Equal(100m, row.TotalRevenue);
        Assert.Equal(1, row.TotalReviewCount);
        Assert.Equal(4d, row.AverageRating!.Value);

        query.MinimumRating = 4.5;
        Assert.Empty(
            (await service.GenerateAsync("admin-token", query)).Providers);
    }

    [Theory]
    [InlineData("Revenue", "Asc", false)]
    [InlineData("Revenue", "Desc", true)]
    [InlineData("AverageRating", "Asc", true)]
    [InlineData("AverageRating", "Desc", false)]
    public async Task SupportsBothSortFieldsAndDirections(
        string sortBy, string direction, bool firstIsHighRevenue)
    {
        using var db = CreateDb();
        var highRevenue = Guid.NewGuid();
        var highRating = Guid.NewGuid();
        var firstListing = Guid.NewGuid();
        var secondListing = Guid.NewGuid();

        db.Bookings.AddRange(
            new Booking
            {
                ListingId = firstListing,
                BookingDate = Day,
                Status = BookingStatus.Confirmed,
                TotalAmount = 200
            },
            new Booking
            {
                ListingId = secondListing,
                BookingDate = Day,
                Status = BookingStatus.Confirmed,
                TotalAmount = 100
            });

        db.ListingReviews.AddRange(
            Review(highRevenue, firstListing, "Experience", 2),
            Review(highRating, secondListing, "Experience", 5));

        await db.SaveChangesAsync();

        var service = new ProviderPerformanceReportService(
            db,
            Catalog(
                Listing(highRevenue, firstListing, "Experience"),
                Listing(highRating, secondListing, "Experience")).Object);

        var report = await service.GenerateAsync("admin-token", new()
        {
            SortBy = sortBy,
            SortDirection = direction
        });

        Assert.Equal(
            firstIsHighRevenue ? highRevenue : highRating,
            report.Providers[0].ProviderId);
    }

    [Fact]
    public async Task RejectsInvalidDateRangeBeforeCatalogLookup()
    {
        using var db = CreateDb();
        var catalog = new Mock<ICatalogService>(MockBehavior.Strict);
        var service = new ProviderPerformanceReportService(db, catalog.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.GenerateAsync("admin-token", new()
            {
                StartDate = Day.AddDays(1),
                EndDate = Day
            }));

        catalog.VerifyNoOtherCalls();
    }

    [Fact]
    public void EndpointRequiresAdminRole()
    {
        var attributes = typeof(ProviderPerformanceReportsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true);

        Assert.Equal(
            "Admin",
            Assert.IsType<AuthorizeAttribute>(Assert.Single(attributes)).Roles);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData("Experience", null, true)]
    [InlineData("Restaurant", null, false)]
    [InlineData(null, 0d, false)]
    public async Task HandlesProviderWithoutListings(
    string? category,
    double? minimumRating,
    bool shouldInclude)
    {
        using var db = CreateDb();

        var providerId = Guid.NewGuid();
        var catalog = Catalog();

        catalog.Setup(c => c.GetReportProvidersAsync(
                "admin-token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CatalogReportProviderResponse>
            {
            new()
            {
                ProviderId = providerId,
                ProviderName = "Provider without listings",
                ServiceType = "tour"
            }
            });

        var service = new ProviderPerformanceReportService(
            db,
            catalog.Object);

        var report = await service.GenerateAsync(
            "admin-token",
            new ProviderPerformanceReportQuery
            {
                ProviderId = providerId,
                Category = category,
                MinimumRating = minimumRating
            });

        if (!shouldInclude)
        {
            Assert.Empty(report.Providers);
            return;
        }

        var row = Assert.Single(report.Providers);

        Assert.Equal(providerId, row.ProviderId);
        Assert.Equal("Provider without listings", row.ProviderName);
        Assert.Contains("Experience", row.Categories);

        Assert.Equal(0m, row.TotalRevenue);
        Assert.Equal(0, row.TotalReviewCount);
        Assert.Null(row.AverageRating);

        Assert.Equal(0, row.StarRatingBreakdown.OneStar);
        Assert.Equal(0, row.StarRatingBreakdown.TwoStars);
        Assert.Equal(0, row.StarRatingBreakdown.ThreeStars);
        Assert.Equal(0, row.StarRatingBreakdown.FourStars);
        Assert.Equal(0, row.StarRatingBreakdown.FiveStars);

        Assert.True(row.NoReviews);
        Assert.False(row.LowRating);
        Assert.Equal("No Reviews", row.RatingState);
    }
}