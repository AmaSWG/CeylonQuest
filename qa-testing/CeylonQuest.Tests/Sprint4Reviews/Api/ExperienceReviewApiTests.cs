using System.Net;
using System.Net.Http.Json;
using CeylonQuest.Tests.Sprint4Notifications.Infrastructure;
using CeylonQuest.Tests.Sprint4Reviews.Infrastructure;

namespace CeylonQuest.Tests.Sprint4Reviews.Api;

/// <summary>
/// Story 12.1 "Submit and View Reviews" - API integration test cases (TC-12.1-API-01..14).
/// Tests all REST endpoints on BookingService ReviewsController against real databases and services.
/// </summary>
[Collection(nameof(ReviewApiCollection))]
public sealed class ExperienceReviewApiTests
{
    private readonly ReviewApiFixture _fixture;

    public ExperienceReviewApiTests(ReviewApiFixture fixture) => _fixture = fixture;

    [Fact(DisplayName = "TC-12.1-API-01: Authentication required")]
    public async Task Api01_AuthenticationRequired()
    {
        var noToken = await _fixture.Reviews.CreateReviewRawAsync(null, new
        {
            bookingId = Guid.NewGuid(),
            bookingType = "Experience",
            rating = 5,
            comment = "Great experience!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);

        var invalidToken = await _fixture.Reviews.CreateReviewRawAsync("not-a-valid-jwt-token", new
        {
            bookingId = Guid.NewGuid(),
            bookingType = "Experience",
            rating = 5,
            comment = "Great experience!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, invalidToken.StatusCode);
    }

    [Fact(DisplayName = "TC-12.1-API-02: S1: Create a review")]
    public async Task Api02_CreateReview()
    {
        var (visitor, bookingId, listingId, _) = await _fixture.CreateCompletedBookingAsync("api02-visitor");
        const string comment = "Wonderful snorkeling experience with friendly guide!";

        var response = await _fixture.Reviews.CreateReviewAsync(visitor.Token, bookingId, "Experience", 4, comment);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var review = await response.Content.ReadFromJsonAsync<ReviewResponse>(JsonDefaults.Options);
        Assert.NotNull(review);
        Assert.Equal(listingId, review.ListingId);
        Assert.Equal("Experience", review.BookingType);
        Assert.Equal(4, review.Rating);
        Assert.Equal(comment, review.Comment);
        Assert.NotEmpty(review.ReviewerDisplayName);
        Assert.True(review.AttendanceVerified);
        Assert.True(review.CreatedAtUtc <= DateTime.UtcNow.AddMinutes(1) && review.CreatedAtUtc >= DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact(DisplayName = "TC-12.1-API-03: S1 and S5: Average and count update")]
    public async Task Api03_AverageAndCountUpdate()
    {
        var (visitor, bookingId, listingId, _) = await _fixture.CreateCompletedBookingAsync("api03-visitor");
        var summaryBefore = await _fixture.Reviews.GetSummaryAsync(listingId, "Experience");
        var initialCount = summaryBefore?.ReviewCount ?? 0;
        var initialAvg = summaryBefore?.AverageRating ?? 0.0;

        const int rating = 5;
        var response = await _fixture.Reviews.CreateReviewAsync(visitor.Token, bookingId, "Experience", rating, "Exceptional service!");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var summaryAfter = await _fixture.Reviews.GetSummaryAsync(listingId, "Experience");
        Assert.NotNull(summaryAfter);
        Assert.Equal(initialCount + 1, summaryAfter.ReviewCount);

        var reviews = await _fixture.Reviews.GetReviewsAsync(listingId, bookingType: "Experience", pageSize: 100);
        if (reviews?.Items.Count > 0)
        {
            var exactAvg = Math.Round(reviews.Items.Average(r => r.Rating), 2, MidpointRounding.AwayFromZero);
            Assert.Equal(exactAvg, summaryAfter.AverageRating, precision: 2);
        }
        else
        {
            Assert.Equal(rating, summaryAfter.AverageRating, precision: 2);
        }
    }

    [Fact(DisplayName = "TC-12.1-API-04: S1: review.submitted event")]
    public async Task Api04_ReviewSubmittedEvent()
    {
        var providerToken = await _fixture.GetProviderTokenAsync();
        var providerUserId = IdentityApiClient.ExtractUserIdFromJwt(providerToken);

        var (visitor, bookingId, listingId, _) = await _fixture.CreateCompletedBookingAsync("api04-visitor", providerUserId);
        const int rating = 4;
        var comment = "Great host and timely service " + Guid.NewGuid().ToString("N")[..6];

        var response = await _fixture.Reviews.CreateReviewAsync(visitor.Token, bookingId, "Experience", rating, comment);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Poll provider notifications
        var providerNotification = await _fixture.Notifications.WaitForNotificationAsync(providerToken,
            n => n.BookingId == bookingId && n.EventType == "review.submitted",
            timeout: TimeSpan.FromSeconds(60));
        Assert.NotNull(providerNotification);
        Assert.Equal(rating, providerNotification.Rating);
        Assert.Contains("New review", providerNotification.Title);

        // Visitor should NOT receive any review.submitted notification
        var visitorList = await _fixture.Notifications.GetListAsync(visitor.Token, pageSize: 50);
        Assert.DoesNotContain(visitorList.Items, n => n.BookingId == bookingId && n.EventType == "review.submitted");
    }

    [Fact(DisplayName = "TC-12.1-API-05: S2: Pending payment booking")]
    public async Task Api05_PendingPaymentBooking_Rejected()
    {
        var (visitor, bookingId, listingId, _) = await _fixture.CreatePendingBookingAsync("api05-visitor");
        var response = await _fixture.Reviews.CreateReviewAsync(visitor.Token, bookingId, "Experience", 5, "Too early review");
        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.Forbidden);

        var reviews = await _fixture.Reviews.GetReviewsAsync(listingId, bookingType: "Experience", pageSize: 50);
        Assert.DoesNotContain(reviews?.Items ?? Array.Empty<ReviewResponse>(), r => r.Comment == "Too early review");
    }

    [Fact(DisplayName = "TC-12.1-API-06: S2: Confirmed but not completed")]
    public async Task Api06_ConfirmedNotCompleted_Rejected()
    {
        var (visitor, bookingId, listingId, _) = await _fixture.CreatePendingBookingAsync("api06-visitor");
        await BookingDbHelper.SetBookingStatusAsync(bookingId, "Confirmed", "Paid");

        var response = await _fixture.Reviews.CreateReviewAsync(visitor.Token, bookingId, "Experience", 5, "Confirmed only review");
        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.Forbidden);

        var reviews = await _fixture.Reviews.GetReviewsAsync(listingId, bookingType: "Experience", pageSize: 50);
        Assert.DoesNotContain(reviews?.Items ?? Array.Empty<ReviewResponse>(), r => r.Comment == "Confirmed only review");
    }

    [Fact(DisplayName = "TC-12.1-API-07: S2: Cancelled booking")]
    public async Task Api07_CancelledBooking_Rejected()
    {
        var (visitor, bookingId, listingId, _) = await _fixture.CreatePendingBookingAsync("api07-visitor");
        await BookingDbHelper.SetBookingStatusAsync(bookingId, "Cancelled", "Unpaid");

        var response = await _fixture.Reviews.CreateReviewAsync(visitor.Token, bookingId, "Experience", 5, "Cancelled booking review");
        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.Forbidden);

        var reviews = await _fixture.Reviews.GetReviewsAsync(listingId, bookingType: "Experience", pageSize: 50);
        Assert.DoesNotContain(reviews?.Items ?? Array.Empty<ReviewResponse>(), r => r.Comment == "Cancelled booking review");
    }

    [Fact(DisplayName = "TC-12.1-API-08: S2: Nonexistent booking")]
    public async Task Api08_NonexistentBooking_NotFound()
    {
        var visitor = await _fixture.Identity.CreateVisitorAsync("api08-visitor");
        var nonExistentBookingId = Guid.NewGuid();

        var response = await _fixture.Reviews.CreateReviewAsync(visitor.Token, nonExistentBookingId, "Experience", 5, "Reviewing a ghost booking");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "TC-12.1-API-09: S2: Another visitor’s booking")]
    public async Task Api09_AnotherVisitorsBooking_Rejected()
    {
        var (visitorA, bookingId, listingId, _) = await _fixture.CreateCompletedBookingAsync("api09-visitor-a");
        var visitorB = await _fixture.Identity.CreateVisitorAsync("api09-visitor-b");

        var response = await _fixture.Reviews.CreateReviewAsync(visitorB.Token, bookingId, "Experience", 5, "Sneaky visitor B review");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var reviews = await _fixture.Reviews.GetReviewsAsync(listingId, bookingType: "Experience", pageSize: 50);
        Assert.DoesNotContain(reviews?.Items ?? Array.Empty<ReviewResponse>(), r => r.Comment == "Sneaky visitor B review");
    }

    [Fact(DisplayName = "TC-12.1-API-10: S4: Duplicate review")]
    public async Task Api10_DuplicateReview_Rejected()
    {
        var (visitor, bookingId, listingId, _) = await _fixture.CreateCompletedBookingAsync("api10-visitor");
        var first = await _fixture.Reviews.CreateReviewAsync(visitor.Token, bookingId, "Experience", 5, "First legitimate review");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _fixture.Reviews.CreateReviewAsync(visitor.Token, bookingId, "Experience", 1, "Duplicate sneaky review");
        Assert.True(second.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest);

        var reviews = await _fixture.Reviews.GetReviewsAsync(listingId, bookingType: "Experience", pageSize: 50);
        Assert.Contains(reviews?.Items ?? Array.Empty<ReviewResponse>(), r => r.Comment == "First legitimate review" && r.Rating == 5);
        Assert.DoesNotContain(reviews?.Items ?? Array.Empty<ReviewResponse>(), r => r.Comment == "Duplicate sneaky review");
    }

    [Fact(DisplayName = "TC-12.1-API-11: Rating validation")]
    public async Task Api11_RatingValidation()
    {
        var (visitor, bookingId, _, _) = await _fixture.CreateCompletedBookingAsync("api11-visitor");

        foreach (var invalidRating in new[] { 0, 6, -1 })
        {
            var res = await _fixture.Reviews.CreateReviewAsync(visitor.Token, bookingId, "Experience", invalidRating, "Invalid rating test");
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        // Missing rating
        var missingRatingRes = await _fixture.Reviews.CreateReviewRawAsync(visitor.Token, new
        {
            bookingId,
            bookingType = "Experience",
            comment = "Missing rating test"
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingRatingRes.StatusCode);
    }

    [Fact(DisplayName = "TC-12.1-API-12: Text and ID validation")]
    public async Task Api12_TextAndIdValidation()
    {
        var (visitor, bookingId, _, _) = await _fixture.CreateCompletedBookingAsync("api12-visitor");

        // Empty text
        var emptyRes = await _fixture.Reviews.CreateReviewAsync(visitor.Token, bookingId, "Experience", 5, "");
        Assert.Equal(HttpStatusCode.BadRequest, emptyRes.StatusCode);

        // Whitespace-only text
        var whitespaceRes = await _fixture.Reviews.CreateReviewAsync(visitor.Token, bookingId, "Experience", 5, "    ");
        Assert.Equal(HttpStatusCode.BadRequest, whitespaceRes.StatusCode);

        // Malformed booking ID ("abc")
        var malformedRes = await _fixture.Reviews.CreateReviewRawAsync(visitor.Token, new
        {
            bookingId = "abc",
            bookingType = "Experience",
            rating = 5,
            comment = "Valid comment with bad id"
        });
        Assert.Equal(HttpStatusCode.BadRequest, malformedRes.StatusCode);
    }

    [Fact(DisplayName = "TC-12.1-API-13: S3: Filter by rating")]
    public async Task Api13_FilterByRating()
    {
        var (visitor1, b1, listingId, _) = await _fixture.CreateCompletedBookingAsync("api13-v1");
        await _fixture.Reviews.CreateReviewAsync(visitor1.Token, b1, "Experience", 5, "5 star review " + Guid.NewGuid().ToString("N")[..6]);

        var (visitor2, b2, _, _) = await _fixture.CreateCompletedBookingAsync("api13-v2");
        await _fixture.Reviews.CreateReviewAsync(visitor2.Token, b2, "Experience", 3, "3 star review " + Guid.NewGuid().ToString("N")[..6]);

        // Filter 5 stars
        var fiveStarList = await _fixture.Reviews.GetReviewsAsync(listingId, rating: 5, bookingType: "Experience");
        Assert.NotNull(fiveStarList);
        Assert.All(fiveStarList.Items, r => Assert.Equal(5, r.Rating));

        // Filter 3 stars
        var threeStarList = await _fixture.Reviews.GetReviewsAsync(listingId, rating: 3, bookingType: "Experience");
        Assert.NotNull(threeStarList);
        Assert.All(threeStarList.Items, r => Assert.Equal(3, r.Rating));

        // Invalid filter: 0
        var zeroFilter = await _fixture.Reviews.GetReviewsRawAsync(listingId, "rating=0&bookingType=Experience");
        Assert.Equal(HttpStatusCode.BadRequest, zeroFilter.StatusCode);

        // Invalid filter: 6
        var sixFilter = await _fixture.Reviews.GetReviewsRawAsync(listingId, "rating=6&bookingType=Experience");
        Assert.Equal(HttpStatusCode.BadRequest, sixFilter.StatusCode);
    }

    [Fact(DisplayName = "TC-12.1-API-14: Role check")]
    public async Task Api14_RoleCheck()
    {
        var (_, bookingId, listingId, _) = await _fixture.CreateCompletedBookingAsync("api14-booking");

        // Provider tries to submit review
        var providerToken = await _fixture.GetProviderTokenAsync();
        var providerRes = await _fixture.Reviews.CreateReviewAsync(providerToken, bookingId, "Experience", 5, "Provider review attempt");
        Assert.Equal(HttpStatusCode.Forbidden, providerRes.StatusCode);

        // Admin tries to submit review
        var adminToken = await _fixture.GetAdminTokenAsync();
        var adminRes = await _fixture.Reviews.CreateReviewAsync(adminToken, bookingId, "Experience", 5, "Admin review attempt");
        Assert.Equal(HttpStatusCode.Forbidden, adminRes.StatusCode);

        var reviews = await _fixture.Reviews.GetReviewsAsync(listingId, bookingType: "Experience", pageSize: 50);
        Assert.DoesNotContain(reviews?.Items ?? Array.Empty<ReviewResponse>(), r => r.Comment.Contains("review attempt"));
    }
}
