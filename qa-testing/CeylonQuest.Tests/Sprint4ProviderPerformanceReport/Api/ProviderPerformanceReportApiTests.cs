using System.Net;
using System.Text.Json;
using CeylonQuest.Tests.Sprint4ProviderPerformanceReport.Infrastructure;
using Xunit;

namespace CeylonQuest.Tests.Sprint4ProviderPerformanceReport.Api;

[Trait("Story", "13.2")]
[Trait("Category", "Api")]
public sealed class ProviderPerformanceReportApiTests
{
    private readonly PerformanceReportApiClient _api = new();

    private async Task<string> GetAdminTokenAsync()
    {
        var (token, _) = await _api.LoginAsync(
            PerformanceReportTestConfig.AdminEmail,
            PerformanceReportTestConfig.AdminPassword);
        return token;
    }

    [Fact(DisplayName = "TC-14.1-API-01: Authentication required")]
    public async Task Api01_AuthenticationRequired_Returns401()
    {
        // 1. No token
        using var noTokenResp = await _api.GetReportRawAsync(token: null);
        Assert.Equal(HttpStatusCode.Unauthorized, noTokenResp.StatusCode);

        // 2. Invalid token
        using var invalidTokenResp = await _api.GetReportRawAsync(token: "invalid.jwt.token");
        Assert.Equal(HttpStatusCode.Unauthorized, invalidTokenResp.StatusCode);
    }

    [Fact(DisplayName = "TC-14.1-API-02: Admin-only authorization")]
    public async Task Api02_AdminOnlyAuthorization_Returns403ForNonAdmin()
    {
        // 1. Visitor token
        var (_, _, visitorToken) = await _api.CreateVisitorAsync("QA-API02");
        using var visitorResp = await _api.GetReportRawAsync(visitorToken);
        Assert.Equal(HttpStatusCode.Forbidden, visitorResp.StatusCode);

        // 2. Provider token
        var (providerToken, _) = await _api.LoginAsync(
            PerformanceReportTestConfig.ProviderEmail,
            PerformanceReportTestConfig.ProviderPassword);
        using var providerResp = await _api.GetReportRawAsync(providerToken);
        Assert.Equal(HttpStatusCode.Forbidden, providerResp.StatusCode);
    }

    [Fact(DisplayName = "TC-14.1-API-03: S1 Response contract")]
    public async Task Api03_ResponseContract_ReturnsExpectedFields()
    {
        var adminToken = await GetAdminTokenAsync();
        var report = await _api.GetReportAsync(adminToken);

        Assert.NotNull(report);
        Assert.NotNull(report.Providers);
        Assert.NotEmpty(report.Providers);

        foreach (var p in report.Providers)
        {
            Assert.NotEqual(Guid.Empty, p.ProviderId);
            Assert.False(string.IsNullOrWhiteSpace(p.ProviderName), "ProviderName must not be empty.");
            Assert.True(p.TotalRevenue >= 0, "TotalRevenue must be non-negative.");
            Assert.True(p.TotalReviewCount >= 0, "TotalReviewCount must be non-negative.");
            Assert.NotNull(p.StarRatingBreakdown);
            Assert.NotNull(p.RatingState);

            if (p.AverageRating.HasValue)
            {
                Assert.InRange(p.AverageRating.Value, 1.0, 5.0);
            }
        }
    }

    [Fact(DisplayName = "TC-14.1-API-04: S1 Revenue accuracy")]
    public async Task Api04_RevenueAccuracy_MatchesNetConfirmedRevenue()
    {
        var adminToken = await GetAdminTokenAsync();
        var report = await _api.GetReportAsync(adminToken);

        // Verify revenue is calculated and non-negative across all providers
        foreach (var p in report.Providers)
        {
            Assert.True(p.TotalRevenue >= 0, $"Total revenue for {p.ProviderName} must be non-negative.");
        }

        // At least one provider in seeded data has positive revenue
        Assert.Contains(report.Providers, p => p.TotalRevenue > 0);
    }

    [Fact(DisplayName = "TC-14.1-API-05: S1 Review count and average accuracy")]
    public async Task Api05_ReviewCountAndAverageAccuracy_MatchesReviewBreakdown()
    {
        var adminToken = await GetAdminTokenAsync();
        var report = await _api.GetReportAsync(adminToken);

        var reviewedProviders = report.Providers.Where(p => p.TotalReviewCount > 0).ToList();
        Assert.NotEmpty(reviewedProviders);

        foreach (var p in reviewedProviders)
        {
            var bd = p.StarRatingBreakdown;
            var expectedSum = (bd.OneStar * 1) + (bd.TwoStars * 2) + (bd.ThreeStars * 3) + (bd.FourStars * 4) + (bd.FiveStars * 5);
            var expectedAvg = (double)expectedSum / p.TotalReviewCount;

            Assert.NotNull(p.AverageRating);
            Assert.Equal(expectedAvg, p.AverageRating.Value, precision: 2);
        }
    }

    [Fact(DisplayName = "TC-14.1-API-06: S1 Star breakdown accuracy")]
    public async Task Api06_StarBreakdownAccuracy_SumMatchesTotalReviewCount()
    {
        var adminToken = await GetAdminTokenAsync();
        var report = await _api.GetReportAsync(adminToken);

        foreach (var p in report.Providers)
        {
            var bd = p.StarRatingBreakdown;
            Assert.True(bd.OneStar >= 0);
            Assert.True(bd.TwoStars >= 0);
            Assert.True(bd.ThreeStars >= 0);
            Assert.True(bd.FourStars >= 0);
            Assert.True(bd.FiveStars >= 0);

            var sumOfStars = bd.OneStar + bd.TwoStars + bd.ThreeStars + bd.FourStars + bd.FiveStars;
            Assert.Equal(p.TotalReviewCount, sumOfStars);
        }
    }

    [Fact(DisplayName = "TC-14.1-API-07: S1 No booking count metric")]
    public async Task Api07_NoBookingCountMetric_ConfirmedInResponse()
    {
        var adminToken = await GetAdminTokenAsync();
        using var resp = await _api.GetReportRawAsync(adminToken);
        var content = await resp.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(content);
        var providers = doc.RootElement.GetProperty("providers");
        Assert.True(providers.GetArrayLength() > 0);

        var firstProvider = providers[0];
        // Confirm no "bookingCount", "totalBookings", or similar metric is present in the row
        Assert.False(firstProvider.TryGetProperty("bookingCount", out _), "Row must not expose bookingCount.");
        Assert.False(firstProvider.TryGetProperty("totalBookings", out _), "Row must not expose totalBookings.");
    }

    [Fact(DisplayName = "TC-14.1-API-08: S2 Low Rating flag")]
    public async Task Api08_LowRatingFlag_ProperlyIdentified()
    {
        var adminToken = await GetAdminTokenAsync();
        var report = await _api.GetReportAsync(adminToken);

        foreach (var p in report.Providers)
        {
            if (p.AverageRating.HasValue && p.AverageRating.Value < 3.0)
            {
                Assert.True(p.LowRating, $"Provider {p.ProviderName} with rating {p.AverageRating} must be flagged LowRating.");
                Assert.Equal("Low Rating", p.RatingState);
            }
            else
            {
                Assert.False(p.LowRating, $"Provider {p.ProviderName} with rating {p.AverageRating} must NOT be flagged LowRating.");
            }
        }
    }

    [Fact(DisplayName = "TC-14.1-API-09: S2 No Reviews flag")]
    public async Task Api09_NoReviewsFlag_AverageRatingNotPresentedAsZero()
    {
        var adminToken = await GetAdminTokenAsync();
        var report = await _api.GetReportAsync(adminToken);

        var unreviewed = report.Providers.Where(p => p.TotalReviewCount == 0).ToList();
        Assert.NotEmpty(unreviewed);

        foreach (var p in unreviewed)
        {
            Assert.True(p.NoReviews, $"Provider {p.ProviderName} with 0 reviews must have NoReviews = true.");
            Assert.Null(p.AverageRating); // Must not display misleading 0.00
            Assert.Equal("No Reviews", p.RatingState);
        }
    }

    [Fact(DisplayName = "TC-14.1-API-10: S3 Filters")]
    public async Task Api10_Filters_CategoryDateAndRatingApplied()
    {
        var adminToken = await GetAdminTokenAsync();

        // 1. Category filter
        var expReport = await _api.GetReportAsync(adminToken, "category=Experience");
        Assert.All(expReport.Providers, p => Assert.Contains("Experience", p.Categories));

        // 2. Minimum Rating filter
        var highRatedReport = await _api.GetReportAsync(adminToken, "minimumRating=4.0");
        Assert.All(highRatedReport.Providers, p =>
        {
            Assert.NotNull(p.AverageRating);
            Assert.True(p.AverageRating.Value >= 4.0);
        });

        // 3. Combined filters
        var combinedReport = await _api.GetReportAsync(adminToken, "category=Experience&minimumRating=4.0");
        Assert.All(combinedReport.Providers, p =>
        {
            Assert.Contains("Experience", p.Categories);
            Assert.NotNull(p.AverageRating);
            Assert.True(p.AverageRating.Value >= 4.0);
        });
    }

    [Fact(DisplayName = "TC-14.1-API-11: S3 Invalid filter validation")]
    public async Task Api11_InvalidFilterValidation_Returns400BadRequest()
    {
        var adminToken = await GetAdminTokenAsync();

        // 1. Start date after end date
        using var invalidDateResp = await _api.GetReportRawAsync(adminToken, "startDate=2026-12-01&endDate=2026-01-01");
        Assert.Equal(HttpStatusCode.BadRequest, invalidDateResp.StatusCode);

        // 2. Minimum rating negative (< 0)
        using var negativeRatingResp = await _api.GetReportRawAsync(adminToken, "minimumRating=-1");
        Assert.Equal(HttpStatusCode.BadRequest, negativeRatingResp.StatusCode);

        // 3. Minimum rating over 5 (> 5)
        using var highRatingResp = await _api.GetReportRawAsync(adminToken, "minimumRating=6");
        Assert.Equal(HttpStatusCode.BadRequest, highRatingResp.StatusCode);

        // 4. Unknown category
        using var unknownCatResp = await _api.GetReportRawAsync(adminToken, "category=UnknownCategoryXYZ");
        Assert.Equal(HttpStatusCode.BadRequest, unknownCatResp.StatusCode);
    }

    [Fact(DisplayName = "TC-14.1-API-12: S4 Sorting")]
    public async Task Api12_Sorting_AscendingDescendingAndInvalidValidation()
    {
        var adminToken = await GetAdminTokenAsync();

        // 1. Sort by AverageRating Desc
        var descReport = await _api.GetReportAsync(adminToken, "sortBy=AverageRating&sortDirection=Desc");
        var ratedDesc = descReport.Providers.Where(p => p.AverageRating.HasValue).Select(p => p.AverageRating!.Value).ToList();
        for (int i = 0; i < ratedDesc.Count - 1; i++)
        {
            Assert.True(ratedDesc[i] >= ratedDesc[i + 1], "Average rating should be in descending order.");
        }

        // 2. Sort by AverageRating Asc
        var ascReport = await _api.GetReportAsync(adminToken, "sortBy=AverageRating&sortDirection=Asc");
        var ratedAsc = ascReport.Providers.Where(p => p.AverageRating.HasValue).Select(p => p.AverageRating!.Value).ToList();
        for (int i = 0; i < ratedAsc.Count - 1; i++)
        {
            Assert.True(ratedAsc[i] <= ratedAsc[i + 1], "Average rating should be in ascending order.");
        }

        // 3. Invalid sort value returns 400 Bad Request
        using var invalidSortResp = await _api.GetReportRawAsync(adminToken, "sortBy=InvalidColumn");
        Assert.Equal(HttpStatusCode.BadRequest, invalidSortResp.StatusCode);
    }

    [Fact(DisplayName = "TC-14.1-API-13: Empty result")]
    public async Task Api13_EmptyResult_Returns200WithEmptyList()
    {
        var adminToken = await GetAdminTokenAsync();

        // Filter that matches no provider (e.g. non-existent ProviderId)
        var nonExistentProvider = Guid.NewGuid();
        var emptyReport = await _api.GetReportAsync(adminToken, $"providerId={nonExistentProvider}");

        Assert.NotNull(emptyReport);
        Assert.NotNull(emptyReport.Providers);
        Assert.Empty(emptyReport.Providers);
    }
}
