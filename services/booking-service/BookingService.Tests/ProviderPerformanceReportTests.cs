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

    //Weighted average, review counts, and provider isolation
    [Fact]
    public async Task CalculatesAverageAcrossReviewsAndKeepsProvidersSeparate()
    {
        using var db = CreateDb();

        var provider = Guid.NewGuid();
        var otherProvider = Guid.NewGuid();
        var firstListing = Guid.NewGuid();
        var secondListing = Guid.NewGuid();
        var otherListing = Guid.NewGuid();

        // Provider ratings: 1, 5, 5, 5.
        // Average must be 4.0, not the average of listing averages (3.0).
        db.ListingReviews.AddRange(
            Review(provider, firstListing, "Experience", 1),
            Review(provider, secondListing, "Experience", 5),
            Review(provider, secondListing, "Experience", 5),
            Review(provider, secondListing, "Experience", 5),
            Review(otherProvider, otherListing, "Experience", 2));

        await db.SaveChangesAsync();

        var service = new ProviderPerformanceReportService(
            db,
            Catalog(
                Listing(provider, firstListing, "Experience"),
                Listing(provider, secondListing, "Experience"),
                Listing(otherProvider, otherListing, "Experience")).Object);

        var report = await service.GenerateAsync("admin-token", new());

        Assert.Equal(2, report.Providers.Count);

        var row = report.Providers.Single(p => p.ProviderId == provider);

        Assert.Equal(4, row.TotalReviewCount);
        Assert.Equal(4d, row.AverageRating!.Value, 10);
        Assert.Equal(1, row.StarRatingBreakdown.OneStar);
        Assert.Equal(0, row.StarRatingBreakdown.TwoStars);
        Assert.Equal(0, row.StarRatingBreakdown.ThreeStars);
        Assert.Equal(0, row.StarRatingBreakdown.FourStars);
        Assert.Equal(3, row.StarRatingBreakdown.FiveStars);

        var other = report.Providers.Single(
            p => p.ProviderId == otherProvider);

        Assert.Equal(1, other.TotalReviewCount);
        Assert.Equal(2d, other.AverageRating!.Value);
    }

    //Date boundaries for every revenue category and review timestamps
    [Fact]
    public async Task IncludesDateBoundariesAndExcludesOutsideDates()
    {
        using var db = CreateDb();

        var provider = Guid.NewGuid();
        var experience = Guid.NewGuid();
        var restaurant = Guid.NewGuid();
        var accommodation = Guid.NewGuid();

        var start = Day;
        var end = Day.AddDays(1);

        foreach (var date in new[]
        {
        start.AddDays(-1),
        start,
        end,
        end.AddDays(1)
    })
        {
            db.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                ListingId = experience,
                BookingDate = date,
                Status = BookingStatus.Completed,
                TotalAmount = 100m,
                RefundAmount = 10m
            });

            db.RestaurantReservations.Add(new RestaurantReservation
            {
                Id = Guid.NewGuid(),
                RestaurantId = restaurant,
                ReservationDate = date,
                Status = ReservationStatus.Completed,
                TotalPrice = 200m,
                RefundAmount = 20m
            });

            db.AccommodationBookings.Add(new AccommodationBooking
            {
                Id = Guid.NewGuid(),
                AccommodationId = accommodation,
                CheckInDate = date,
                CheckOutDate = date.AddDays(1),
                Status = AccommodationBookingStatus.Completed,
                TotalPrice = 300m,
                RefundAmount = 30m
            });
        }

        var startUtc = start.ToDateTime(
            TimeOnly.MinValue, DateTimeKind.Utc);

        var endExclusiveUtc = end.AddDays(1).ToDateTime(
            TimeOnly.MinValue, DateTimeKind.Utc);

        var beforeStart = Review(provider, experience, "Experience", 1);
        beforeStart.CreatedAtUtc = startUtc.AddTicks(-1);

        var atStart = Review(provider, experience, "Experience", 4);
        atStart.CreatedAtUtc = startUtc;

        var beforeEnd = Review(provider, experience, "Experience", 5);
        beforeEnd.CreatedAtUtc = endExclusiveUtc.AddTicks(-1);

        var afterEnd = Review(provider, experience, "Experience", 1);
        afterEnd.CreatedAtUtc = endExclusiveUtc;

        db.ListingReviews.AddRange(
            beforeStart, atStart, beforeEnd, afterEnd);

        await db.SaveChangesAsync();

        var service = new ProviderPerformanceReportService(
            db,
            Catalog(
                Listing(provider, experience, "Experience"),
                Listing(provider, restaurant, "Restaurant"),
                Listing(provider, accommodation, "Accommodation")).Object);

        var row = Assert.Single(
            (await service.GenerateAsync("admin-token", new()
            {
                StartDate = start,
                EndDate = end
            })).Providers);

        // Two included dates: 2 × (90 + 180 + 270).
        Assert.Equal(1080m, row.TotalRevenue);
        Assert.Equal(2, row.TotalReviewCount);
        Assert.Equal(4.5d, row.AverageRating!.Value);
        Assert.Equal(0, row.StarRatingBreakdown.OneStar);
        Assert.Equal(1, row.StarRatingBreakdown.FourStars);
        Assert.Equal(1, row.StarRatingBreakdown.FiveStars);
    }

    //Independent category filtering
    [Theory]
    [InlineData("experience", 90, 2)]
    [InlineData("Restaurant", 180, 4)]
    [InlineData("Accommodation", 270, 5)]
    public async Task CategoryFilterScopesRevenueAndReviews(
    string category,
    int expectedRevenue,
    int expectedRating)
    {
        using var db = CreateDb();

        var provider = Guid.NewGuid();
        var experience = Guid.NewGuid();
        var restaurant = Guid.NewGuid();
        var accommodation = Guid.NewGuid();
        var unrelatedProvider = Guid.NewGuid();

        db.Bookings.Add(new Booking
        {
            ListingId = experience,
            BookingDate = Day,
            Status = BookingStatus.Completed,
            TotalAmount = 100m,
            RefundAmount = 10m
        });

        db.RestaurantReservations.Add(new RestaurantReservation
        {
            RestaurantId = restaurant,
            ReservationDate = Day,
            Status = ReservationStatus.Completed,
            TotalPrice = 200m,
            RefundAmount = 20m
        });

        db.AccommodationBookings.Add(new AccommodationBooking
        {
            AccommodationId = accommodation,
            CheckInDate = Day,
            CheckOutDate = Day.AddDays(1),
            Status = AccommodationBookingStatus.Completed,
            TotalPrice = 300m,
            RefundAmount = 30m
        });

        db.ListingReviews.AddRange(
            Review(provider, experience, "Experience", 2),
            Review(provider, restaurant, "Restaurant", 4),
            Review(provider, accommodation, "Accommodation", 5));

        await db.SaveChangesAsync();

        // The unrelated provider has a different category and must be excluded.
        var unrelatedCategory = category.Equals(
            "Restaurant", StringComparison.OrdinalIgnoreCase)
            ? "Experience"
            : "Restaurant";

        var service = new ProviderPerformanceReportService(
            db,
            Catalog(
                Listing(provider, experience, "Experience"),
                Listing(provider, restaurant, "Restaurant"),
                Listing(provider, accommodation, "Accommodation"),
                Listing(
                    unrelatedProvider,
                    Guid.NewGuid(),
                    unrelatedCategory)).Object);

        var row = Assert.Single(
            (await service.GenerateAsync("admin-token", new()
            {
                Category = category
            })).Providers);

        Assert.Equal(provider, row.ProviderId);
        Assert.Equal((decimal)expectedRevenue, row.TotalRevenue);
        Assert.Equal(1, row.TotalReviewCount);
        Assert.Equal((double)expectedRating, row.AverageRating!.Value);

        var returnedCategory = Assert.Single(row.Categories);

        Assert.True(returnedCategory.Equals(
            category, StringComparison.OrdinalIgnoreCase));
    }

    //Minimum-rating boundary and unrated-provider exclusion
    [Theory]
    [InlineData(0d, 2)]
    [InlineData(3d, 2)]
    [InlineData(3.1d, 1)]
    [InlineData(5d, 1)]
    public async Task MinimumRatingIncludesBoundaryAndExcludesUnratedProviders(
    double minimumRating,
    int expectedCount)
    {
        using var db = CreateDb();

        var boundaryProvider = Guid.NewGuid();
        var highProvider = Guid.NewGuid();
        var unratedProvider = Guid.NewGuid();

        var boundaryListing = Guid.NewGuid();
        var highListing = Guid.NewGuid();

        db.ListingReviews.AddRange(
            Review(boundaryProvider, boundaryListing, "Experience", 3),
            Review(highProvider, highListing, "Experience", 5));

        await db.SaveChangesAsync();

        var service = new ProviderPerformanceReportService(
            db,
            Catalog(
                Listing(boundaryProvider, boundaryListing, "Experience"),
                Listing(highProvider, highListing, "Experience"),
                Listing(unratedProvider, Guid.NewGuid(), "Experience")).Object);

        var report = await service.GenerateAsync("admin-token", new()
        {
            MinimumRating = minimumRating
        });

        Assert.Equal(expectedCount, report.Providers.Count);

        Assert.DoesNotContain(
            report.Providers,
            p => p.ProviderId == unratedProvider);

        Assert.All(report.Providers, p =>
            Assert.True(p.AverageRating!.Value >= minimumRating));
    }

    //Low Rating boundary
    [Theory]
    [InlineData(2, 3, 2.5d, true, "Low Rating")]
    [InlineData(3, 3, 3d, false, "Rated")]
    [InlineData(3, 4, 3.5d, false, "Rated")]
    public async Task LowRatingUsesStrictlyBelowThree(
    int firstRating,
    int secondRating,
    double expectedAverage,
    bool expectedLowRating,
    string expectedState)
    {
        using var db = CreateDb();

        var provider = Guid.NewGuid();
        var listing = Guid.NewGuid();

        db.ListingReviews.AddRange(
            Review(provider, listing, "Experience", firstRating),
            Review(provider, listing, "Experience", secondRating));

        await db.SaveChangesAsync();

        var service = new ProviderPerformanceReportService(
            db,
            Catalog(Listing(provider, listing, "Experience")).Object);

        var row = Assert.Single(
            (await service.GenerateAsync("admin-token", new())).Providers);

        Assert.Equal(expectedAverage, row.AverageRating!.Value);
        Assert.Equal(expectedLowRating, row.LowRating);
        Assert.Equal(expectedState, row.RatingState);
        Assert.False(row.NoReviews);
    }

    //Unrated providers sort last in either rating direction
    [Theory]
    [InlineData("Asc")]
    [InlineData("Desc")]
    public async Task RatingSortingPlacesUnratedProvidersLast(string direction)
    {
        using var db = CreateDb();

        var lowProvider = Guid.NewGuid();
        var highProvider = Guid.NewGuid();
        var unratedProvider = Guid.NewGuid();

        var lowListing = Guid.NewGuid();
        var highListing = Guid.NewGuid();

        db.ListingReviews.AddRange(
            Review(lowProvider, lowListing, "Experience", 2),
            Review(highProvider, highListing, "Experience", 5));

        await db.SaveChangesAsync();

        var service = new ProviderPerformanceReportService(
            db,
            Catalog(
                Listing(lowProvider, lowListing, "Experience"),
                Listing(highProvider, highListing, "Experience"),
                Listing(unratedProvider, Guid.NewGuid(), "Experience")).Object);

        var report = await service.GenerateAsync("admin-token", new()
        {
            SortBy = "AverageRating",
            SortDirection = direction
        });

        var expectedOrder = direction == "Asc"
            ? new[] { lowProvider, highProvider, unratedProvider }
            : new[] { highProvider, lowProvider, unratedProvider };

        Assert.Equal(
            expectedOrder,
            report.Providers.Select(p => p.ProviderId).ToArray());
    }

    //Booking count does not affect rating metrics or appear in JSON
    [Fact]
    public async Task BookingVolumeDoesNotChangeRatingMetricsOrExposeBookingCount()
    {
        using var db = CreateDb();

        var provider = Guid.NewGuid();
        var listing = Guid.NewGuid();

        db.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            ListingId = listing,
            BookingDate = Day,
            Status = BookingStatus.Completed,
            TotalAmount = 100m
        });

        db.ListingReviews.AddRange(
            Review(provider, listing, "Experience", 2),
            Review(provider, listing, "Experience", 5));

        await db.SaveChangesAsync();

        var service = new ProviderPerformanceReportService(
            db,
            Catalog(Listing(provider, listing, "Experience")).Object);

        var before = Assert.Single(
            (await service.GenerateAsync("admin-token", new())).Providers);

        // Increase booking volume without changing revenue or reviews.
        for (var i = 0; i < 10; i++)
        {
            db.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                ListingId = listing,
                BookingDate = Day,
                Status = BookingStatus.Completed,
                TotalAmount = 0m
            });
        }

        await db.SaveChangesAsync();

        var afterReport = await service.GenerateAsync("admin-token", new());
        var after = Assert.Single(afterReport.Providers);

        Assert.Equal(100m, after.TotalRevenue);
        Assert.Equal(2, after.TotalReviewCount);
        Assert.Equal(3.5d, after.AverageRating!.Value);

        Assert.Equal(before.TotalRevenue, after.TotalRevenue);
        Assert.Equal(before.TotalReviewCount, after.TotalReviewCount);
        Assert.Equal(before.AverageRating, after.AverageRating);

        Assert.Equal(
            before.StarRatingBreakdown.OneStar,
            after.StarRatingBreakdown.OneStar);
        Assert.Equal(
            before.StarRatingBreakdown.TwoStars,
            after.StarRatingBreakdown.TwoStars);
        Assert.Equal(
            before.StarRatingBreakdown.ThreeStars,
            after.StarRatingBreakdown.ThreeStars);
        Assert.Equal(
            before.StarRatingBreakdown.FourStars,
            after.StarRatingBreakdown.FourStars);
        Assert.Equal(
            before.StarRatingBreakdown.FiveStars,
            after.StarRatingBreakdown.FiveStars);

        Assert.Equal(before.NoReviews, after.NoReviews);
        Assert.Equal(before.LowRating, after.LowRating);

        var json = System.Text.Json.JsonSerializer.Serialize(
            afterReport,
            new System.Text.Json.JsonSerializerOptions(
                System.Text.Json.JsonSerializerDefaults.Web));

        Assert.False(json.Contains(
            "bookingCount",
            StringComparison.OrdinalIgnoreCase));
    }

    //Empty catalog and invalid filter values
    [Fact]
    public async Task EmptyCatalogReturnsEmptyReport()
    {
        using var db = CreateDb();

        var service = new ProviderPerformanceReportService(
            db, Catalog().Object);

        var report = await service.GenerateAsync("admin-token", new());

        Assert.Empty(report.Providers);
    }

    [Theory]
    [InlineData("MinimumRating", "-1")]
    [InlineData("MinimumRating", "6")]
    [InlineData("Category", "InvalidCategory")]
    [InlineData("SortBy", "BookingCount")]
    [InlineData("SortDirection", "InvalidDirection")]
    public async Task RejectsInvalidFiltersBeforeCallingCatalog(
        string field,
        string value)
    {
        using var db = CreateDb();

        var catalog = new Mock<ICatalogService>(MockBehavior.Strict);
        var service = new ProviderPerformanceReportService(
            db, catalog.Object);

        var query = new ProviderPerformanceReportQuery();

        switch (field)
        {
            case "MinimumRating":
                query.MinimumRating = double.Parse(
                    value,
                    System.Globalization.CultureInfo.InvariantCulture);
                break;
            case "Category":
                query.Category = value;
                break;
            case "SortBy":
                query.SortBy = value;
                break;
            case "SortDirection":
                query.SortDirection = value;
                break;
        }

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.GenerateAsync("admin-token", query));

        catalog.VerifyNoOtherCalls();
    }
}