using System.Text.Json;
using CeylonQuest.Tests.Sprint4Notifications;
using CeylonQuest.Tests.Sprint4Notifications.Infrastructure;
using CeylonQuest.Tests.Sprint4Reviews.Infrastructure;
using OpenQA.Selenium;

namespace CeylonQuest.Tests.Sprint4Reviews.Ui;

/// <summary>
/// Story 12.1 "Review an Experience" - Selenium UI test cases (TC-12.1-UI-01..12).
/// Real browser automation against the real backend services and database.
/// </summary>
[Collection(nameof(ReviewUiCollection))]
public sealed class ExperienceReviewUiTests
{
    private readonly ReviewUiFixture _fixture;

    public ExperienceReviewUiTests(ReviewUiFixture fixture) => _fixture = fixture;

    /// <summary>Registers a fresh visitor and creates one real Experience booking (PendingPayment) via the real API.</summary>
    private async Task<(TestUser Visitor, Guid BookingId, string ListingTitle)> CreatePendingBookingAsync(string label)
    {
        var visitor = await _fixture.Identity.CreateVisitorAsync(label);
        var (bookingId, listingTitle) = await CreatePendingBookingAsync(visitor);
        return (visitor, bookingId, listingTitle);
    }

    /// <summary>Creates one real Experience booking (PendingPayment) for an existing visitor via the real API.</summary>
    private async Task<(Guid BookingId, string ListingTitle)> CreatePendingBookingAsync(TestUser visitor)
    {
        var listing = await _fixture.Catalog.FindBookableListingAsync("Experience")
            ?? throw new InvalidOperationException("No bookable Experience listing found - seed at least one active, approved experience to run this suite.");
        var slot = await _fixture.Catalog.FindAvailableSlotAsync(listing.Id)
            ?? throw new InvalidOperationException($"No available slot found for listing {listing.Id} in the next 35 days.");

        HttpResponseMessage? response = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            response = await _fixture.Booking.CreateExperienceBookingAsync(visitor.Token, listing.Id, slot.Date, slot.TimeSlot, 1);
            if (response.IsSuccessStatusCode) break;
            if (attempt < 2) await Task.Delay(1500);
        }

        if (response == null || !response.IsSuccessStatusCode)
        {
            var err = response != null ? await response.Content.ReadAsStringAsync() : "No response";
            throw new HttpRequestException($"Booking creation failed with status {response?.StatusCode}: {err}");
        }
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var bookingId = body.RootElement.GetProperty("id").GetGuid();

        return (bookingId, listing.Title);
    }

    /// <summary>Registers a fresh visitor and creates one real completed Experience booking via API + direct DB state update.</summary>
    private async Task<(TestUser Visitor, Guid BookingId, string ListingTitle)> CreateCompletedBookingAsync(string label, Guid? providerUserId = null)
    {
        var (visitor, bookingId, listingTitle) = await CreatePendingBookingAsync(label);
        await BookingDbHelper.MarkBookingCompletedAsync(bookingId, providerUserId);
        return (visitor, bookingId, listingTitle);
    }

    /// <summary>Finds the single booking row for a listing title, optionally disambiguating by a required substring (e.g. a status) when the visitor has more than one booking for the same listing.</summary>
    private static IWebElement? FindBookingRow(IWebDriver driver, string listingTitle, string? mustContain = null)
    {
        var rows = driver.FindElements(By.XPath($"//tr[.//strong[contains(normalize-space(.), {XPathLiteral(listingTitle)})]]"));
        return mustContain == null
            ? rows.FirstOrDefault()
            : rows.FirstOrDefault(r => r.Text.Contains(mustContain, StringComparison.OrdinalIgnoreCase));
    }

    // Builds a safe XPath string literal even if the title itself contains a single quote.
    private static string XPathLiteral(string value) =>
        !value.Contains('\'') ? $"'{value}'" : $"concat('{value.Replace("'", "', \"'\", '")}')";

    private IWebElement WaitForCompletedBookingRow(string title)
    {
        Eventually.Assert(() => FindBookingRow(_fixture.Driver, title, "Completed") != null,
            message: $"Expected completed booking row for '{title}' to render within timeout.");
        return FindBookingRow(_fixture.Driver, title, "Completed")!;
    }

    [Fact(DisplayName = "TC-12.1-UI-01: review option only for completed bookings (Pending/Cancelled reachable in this environment)")]
    public async Task Ui01_ReviewOptionOnlyForCompletedBookings()
    {
        var (visitor, _, pendingTitle) = await CreatePendingBookingAsync("ui01-visitor");
        var (cancelledBookingId, cancelledTitle) = await CreatePendingBookingAsync(visitor);
        (await _fixture.Booking.CancelBookingAsync(visitor.Token, cancelledBookingId, "QA test cancellation")).EnsureSuccessStatusCode();

        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenBookings();

        Eventually.Assert(() =>
        {
            var row = FindBookingRow(_fixture.Driver, pendingTitle, mustContain: "PendingPayment");
            return row != null && row.Text.Contains("completed bookings can be reviewed", StringComparison.OrdinalIgnoreCase);
        });
        var pendingRow = FindBookingRow(_fixture.Driver, pendingTitle, mustContain: "PendingPayment")
            ?? throw new NoSuchElementException($"No pending booking row found for '{pendingTitle}'");
        Assert.Empty(pendingRow.FindElements(By.XPath(".//button[contains(.,'Write Review')]")));
        Assert.Contains("completed bookings can be reviewed", pendingRow.Text, StringComparison.OrdinalIgnoreCase);

        Eventually.Assert(() => FindBookingRow(_fixture.Driver, cancelledTitle, mustContain: "Cancelled") != null);
        var cancelledRow = FindBookingRow(_fixture.Driver, cancelledTitle, mustContain: "Cancelled")
            ?? throw new NoSuchElementException($"No cancelled booking row found for '{cancelledTitle}'");
        Assert.Empty(cancelledRow.FindElements(By.XPath(".//button[contains(.,'Write Review')]")));
    }

    [Fact(DisplayName = "TC-12.1-UI-02: review form validation")]
    public async Task Ui02_ReviewFormValidation()
    {
        var (visitor, bookingId, title) = await CreateCompletedBookingAsync("ui02-visitor");
        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenBookings();

        var row = WaitForCompletedBookingRow(title);
        row.FindElement(By.XPath(".//button[contains(.,'Write Review')]")).Click();

        var wait = DriverFactory.Wait(_fixture.Driver);
        wait.Until(d => d.FindElements(By.CssSelector("textarea")).Count > 0);

        var submitBtn = _fixture.Driver.FindElement(By.CssSelector(".cq-review-form button[type='submit']"));
        Assert.False(submitBtn.Enabled, "Submit Review button should be disabled when rating and feedback are empty.");

        // Select a star rating (e.g. 4 stars)
        SelectStarRating(4);
        Assert.False(submitBtn.Enabled, "Submit Review should remain disabled until comments are provided.");

        // Provide review comment
        var textarea = _fixture.Driver.FindElement(By.CssSelector("textarea"));
        textarea.SendKeys("Amazing experience with great support!");
        Assert.True(submitBtn.Enabled, "Submit Review should be enabled once rating and comment are provided.");
    }

    [Fact(DisplayName = "TC-12.1-UI-03: S1 submit a review")]
    public async Task Ui03_SubmitReview()
    {
        var (visitor, bookingId, title) = await CreateCompletedBookingAsync("ui03-visitor");
        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenBookings();

        var row = WaitForCompletedBookingRow(title);
        row.FindElement(By.XPath(".//button[contains(.,'Write Review')]")).Click();

        var waitForm = DriverFactory.Wait(_fixture.Driver);
        waitForm.Until(d => d.FindElements(By.CssSelector("textarea")).Count > 0);

        SelectStarRating(5);

        var uniqueComment = "Unforgettable journey! " + Guid.NewGuid().ToString("N")[..8];
        var textarea = _fixture.Driver.FindElement(By.CssSelector("textarea"));
        textarea.SendKeys(uniqueComment);

        _fixture.Driver.FindElement(By.CssSelector(".cq-review-form button[type='submit']")).Click();

        Eventually.Assert(() =>
        {
            var updatedRow = FindBookingRow(_fixture.Driver, title, "Completed");
            return updatedRow != null && updatedRow.Text.Contains("Review Submitted", StringComparison.OrdinalIgnoreCase);
        });

        // Verify review appears in public listing details
        _fixture.Driver.FindElement(By.Id("nav-explore")).Click();
        var wait = DriverFactory.Wait(_fixture.Driver);
        wait.Until(d => d.FindElements(By.CssSelector(".vd-service-card")).Count > 0);

        var card = FindExploreCard(title);
        card.FindElement(By.XPath(".//button[contains(.,'View Details')]")).Click();
        wait.Until(d => d.FindElements(By.CssSelector(".cq-reviews")).Count > 0);

        Eventually.Assert(() =>
        {
            var reviewsSection = _fixture.Driver.FindElement(By.CssSelector(".cq-reviews"));
            return reviewsSection != null && reviewsSection.Text.Contains(uniqueComment, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact(DisplayName = "TC-12.1-UI-04: S1/S5 rating and count update on the detail page")]
    public async Task Ui04_RatingAndCountUpdate()
    {
        var (visitor, bookingId, title) = await CreateCompletedBookingAsync("ui04-visitor");
        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenBookings();

        var row = WaitForCompletedBookingRow(title);
        row.FindElement(By.XPath(".//button[contains(.,'Write Review')]")).Click();

        var waitForm = DriverFactory.Wait(_fixture.Driver);
        waitForm.Until(d => d.FindElements(By.CssSelector("textarea")).Count > 0);

        SelectStarRating(5);

        var textarea = _fixture.Driver.FindElement(By.CssSelector("textarea"));
        textarea.SendKeys("Outstanding experience, loved every moment!");
        _fixture.Driver.FindElement(By.CssSelector(".cq-review-form button[type='submit']")).Click();

        Eventually.Assert(() =>
        {
            var updatedRow = FindBookingRow(_fixture.Driver, title, "Completed");
            return updatedRow != null && updatedRow.Text.Contains("Review Submitted", StringComparison.OrdinalIgnoreCase);
        });

        _fixture.Driver.FindElement(By.Id("nav-explore")).Click();
        var wait = DriverFactory.Wait(_fixture.Driver);
        wait.Until(d => d.FindElements(By.CssSelector(".vd-service-card")).Count > 0);

        var card = FindExploreCard(title);
        card.FindElement(By.XPath(".//button[contains(.,'View Details')]")).Click();
        wait.Until(d => d.FindElements(By.CssSelector(".cq-reviews")).Count > 0);

        Eventually.Assert(() =>
        {
            var section = _fixture.Driver.FindElement(By.CssSelector(".cq-reviews"));
            return section != null && !section.Text.Contains("No reviews yet", StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact(DisplayName = "TC-12.1-UI-05: S5 rating on the listing card")]
    public async Task Ui05_RatingOnListingCard()
    {
        var (visitor, bookingId, title) = await CreateCompletedBookingAsync("ui05-visitor");
        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenBookings();

        var row = WaitForCompletedBookingRow(title);
        row.FindElement(By.XPath(".//button[contains(.,'Write Review')]")).Click();

        var waitForm = DriverFactory.Wait(_fixture.Driver);
        waitForm.Until(d => d.FindElements(By.CssSelector("textarea")).Count > 0);

        SelectStarRating(5);

        var textarea = _fixture.Driver.FindElement(By.CssSelector("textarea"));
        textarea.SendKeys("Rating update listing card test!");
        _fixture.Driver.FindElement(By.CssSelector(".cq-review-form button[type='submit']")).Click();

        Eventually.Assert(() =>
        {
            var updatedRow = FindBookingRow(_fixture.Driver, title, "Completed");
            return updatedRow != null && updatedRow.Text.Contains("Review Submitted", StringComparison.OrdinalIgnoreCase);
        });

        _fixture.Driver.FindElement(By.Id("nav-explore")).Click();
        var wait = DriverFactory.Wait(_fixture.Driver);
        wait.Until(d => d.FindElements(By.CssSelector(".vd-service-card")).Count > 0);

        Eventually.Assert(() =>
        {
            var card = FindExploreCard(title);
            return card != null && card.Text.Contains("5");
        });
    }

    [Fact(DisplayName = "TC-12.1-UI-06: S2 pending payment booking shows no review action")]
    public async Task Ui06_PendingPaymentBooking_NoReviewAction()
    {
        var (visitor, _, title) = await CreatePendingBookingAsync("ui06-pending");

        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenBookings();

        Eventually.Assert(() =>
        {
            var r = FindBookingRow(_fixture.Driver, title);
            return r != null && r.Text.Contains("completed bookings can be reviewed", StringComparison.OrdinalIgnoreCase);
        });
        var row = FindBookingRow(_fixture.Driver, title)
            ?? throw new NoSuchElementException($"No booking row found for '{title}'");
        Assert.Empty(row.FindElements(By.XPath(".//button[contains(.,'Write Review')]")));
        Assert.Contains("completed bookings can be reviewed", row.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "TC-12.1-UI-07: S2 cancelled booking shows no review action")]
    public async Task Ui07_CancelledBooking_NoReviewAction()
    {
        var (visitor, bookingId, title) = await CreatePendingBookingAsync("ui07-cancelled");
        (await _fixture.Booking.CancelBookingAsync(visitor.Token, bookingId, "QA test cancellation")).EnsureSuccessStatusCode();

        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenBookings();

        Eventually.Assert(() => FindBookingRow(_fixture.Driver, title) != null);
        var row = FindBookingRow(_fixture.Driver, title)
            ?? throw new NoSuchElementException($"No booking row found for '{title}'");
        Assert.Empty(row.FindElements(By.XPath(".//button[contains(.,'Write Review')]")));
    }

    [Fact(DisplayName = "TC-12.1-UI-08: S4 duplicate review")]
    public async Task Ui08_DuplicateReview()
    {
        var (visitor, bookingId, title) = await CreateCompletedBookingAsync("ui08-visitor");
        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenBookings();

        var row = WaitForCompletedBookingRow(title);
        row.FindElement(By.XPath(".//button[contains(.,'Write Review')]")).Click();

        var waitForm = DriverFactory.Wait(_fixture.Driver);
        waitForm.Until(d => d.FindElements(By.CssSelector("textarea")).Count > 0);

        SelectStarRating(5);

        var textarea = _fixture.Driver.FindElement(By.CssSelector("textarea"));
        textarea.SendKeys("First review submission for duplicate prevention test.");
        _fixture.Driver.FindElement(By.CssSelector(".cq-review-form button[type='submit']")).Click();

        Eventually.Assert(() =>
        {
            var r = FindBookingRow(_fixture.Driver, title, "Completed");
            return r != null && r.Text.Contains("Review Submitted", StringComparison.OrdinalIgnoreCase);
        });

        // Refresh bookings and verify "Write Review" is no longer available, "Review Submitted" is shown
        _fixture.Shell.Refresh();
        _fixture.Shell.OpenBookings();

        var refreshedRow = WaitForCompletedBookingRow(title);
        Assert.Empty(refreshedRow.FindElements(By.XPath(".//button[contains(.,'Write Review')]")));
        Assert.Contains("Review Submitted", refreshedRow.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "TC-12.1-UI-09: S3 star filter")]
    public async Task Ui09_StarFilter()
    {
        var (visitor, bookingId, title) = await CreateCompletedBookingAsync("ui09-visitor");
        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenBookings();

        var row = WaitForCompletedBookingRow(title);
        row.FindElement(By.XPath(".//button[contains(.,'Write Review')]")).Click();

        var waitForm = DriverFactory.Wait(_fixture.Driver);
        waitForm.Until(d => d.FindElements(By.CssSelector("textarea")).Count > 0);

        SelectStarRating(5);

        var uniqueComment = "Star filter 5-star review " + Guid.NewGuid().ToString("N")[..8];
        var textarea = _fixture.Driver.FindElement(By.CssSelector("textarea"));
        textarea.SendKeys(uniqueComment);
        _fixture.Driver.FindElement(By.CssSelector(".cq-review-form button[type='submit']")).Click();

        Eventually.Assert(() =>
        {
            var r = FindBookingRow(_fixture.Driver, title, "Completed");
            return r != null && r.Text.Contains("Review Submitted", StringComparison.OrdinalIgnoreCase);
        });

        // Navigate to details page and exercise star filter
        _fixture.Driver.FindElement(By.Id("nav-explore")).Click();
        var wait = DriverFactory.Wait(_fixture.Driver);
        wait.Until(d => d.FindElements(By.CssSelector(".vd-service-card")).Count > 0);

        var card = FindExploreCard(title);
        card.FindElement(By.XPath(".//button[contains(.,'View Details')]")).Click();
        wait.Until(d => d.FindElements(By.CssSelector(".cq-reviews")).Count > 0);

        FilterReviewsByRating("5");

        Eventually.Assert(() =>
        {
            var section = _fixture.Driver.FindElement(By.CssSelector(".cq-reviews"));
            return section != null && section.Text.Contains(uniqueComment, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact(DisplayName = "TC-12.1-UI-10: S3 filter with no matches and reset")]
    public async Task Ui10_StarFilterEmptyAndReset()
    {
        var (visitor, bookingId, title) = await CreateCompletedBookingAsync("ui10-visitor");
        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenBookings();

        var row = WaitForCompletedBookingRow(title);
        row.FindElement(By.XPath(".//button[contains(.,'Write Review')]")).Click();

        var waitForm = DriverFactory.Wait(_fixture.Driver);
        waitForm.Until(d => d.FindElements(By.CssSelector("textarea")).Count > 0);

        SelectStarRating(5);

        var uniqueComment = "Five-star test review for filter reset " + Guid.NewGuid().ToString("N")[..8];
        var textarea = _fixture.Driver.FindElement(By.CssSelector("textarea"));
        textarea.SendKeys(uniqueComment);
        _fixture.Driver.FindElement(By.CssSelector(".cq-review-form button[type='submit']")).Click();

        Eventually.Assert(() =>
        {
            var r = FindBookingRow(_fixture.Driver, title, "Completed");
            return r != null && r.Text.Contains("Review Submitted", StringComparison.OrdinalIgnoreCase);
        });

        _fixture.Driver.FindElement(By.Id("nav-explore")).Click();
        var wait = DriverFactory.Wait(_fixture.Driver);
        wait.Until(d => d.FindElements(By.CssSelector(".vd-service-card")).Count > 0);

        var card = FindExploreCard(title);
        card.FindElement(By.XPath(".//button[contains(.,'View Details')]")).Click();
        wait.Until(d => d.FindElements(By.CssSelector(".cq-reviews")).Count > 0);

        // Filter by 1 star (which has no matches)
        FilterReviewsByRating("1");
        Eventually.Assert(() =>
        {
            var section = _fixture.Driver.FindElement(By.CssSelector(".cq-reviews"));
            return section != null && section.Text.Contains("No reviews match this star rating", StringComparison.OrdinalIgnoreCase);
        });

        // Reset filter back to All ratings
        FilterReviewsByRating("");
        Eventually.Assert(() =>
        {
            var section = _fixture.Driver.FindElement(By.CssSelector(".cq-reviews"));
            return section != null && section.Text.Contains(uniqueComment, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact(DisplayName = "TC-12.1-UI-11: no reviews yet shows an honest empty state, not a misleading average")]
    public async Task Ui11_NoReviewsYet()
    {
        var visitor = await _fixture.Identity.CreateVisitorAsync("ui11-visitor");
        var listings = await _fixture.Catalog.FindBookableListingsAsync("Experience");
        CatalogSearchItem? listing = null;
        foreach (var l in listings)
        {
            var s = await _fixture.Reviews.GetSummaryAsync(l.Id, "Experience");
            if (s == null || s.ReviewCount == 0)
            {
                listing = l;
                break;
            }
        }

        if (listing == null)
            throw new InvalidOperationException("No unreviewed Experience listing found - seed at least one active, approved experience to run this suite.");

        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Driver.FindElement(By.Id("nav-explore")).Click();
        var wait = DriverFactory.Wait(_fixture.Driver);
        wait.Until(d => d.FindElements(By.CssSelector(".vd-service-card")).Count > 0);

        var card = FindExploreCard(listing.Title);
        card.FindElement(By.XPath(".//button[contains(.,'View Details')]")).Click();
        wait.Until(d => d.FindElements(By.CssSelector(".cq-reviews")).Count > 0);

        Eventually.Assert(() =>
        {
            var sec = _fixture.Driver.FindElement(By.CssSelector(".cq-reviews"));
            return sec != null && sec.Text.Contains("No reviews yet", StringComparison.OrdinalIgnoreCase);
        });
        var section = _fixture.Driver.FindElement(By.CssSelector(".cq-reviews"));
        Assert.Contains("No reviews yet", section.Text);
        Assert.DoesNotContain("0.00 / 5", section.Text);
        Assert.DoesNotContain("NaN", section.Text);
    }

    [Fact(DisplayName = "TC-12.1-UI-12: S1 provider notification (cross-story with 11.1)")]
    public async Task Ui12_ProviderNotification()
    {
        var providerToken = await _fixture.Identity.LoginAsync(TestConfig.ProviderEmail, TestConfig.ProviderPassword);
        var providerUserId = IdentityApiClient.ExtractUserIdFromJwt(providerToken);

        var (visitor, bookingId, title) = await CreateCompletedBookingAsync("ui12-visitor", providerUserId);

        var uniqueComment = "Provider notification test " + Guid.NewGuid().ToString("N")[..8];
        var response = await _fixture.Reviews.CreateReviewAsync(visitor.Token, bookingId, "Experience", 5, uniqueComment);
        response.EnsureSuccessStatusCode();

        // Wait for notification to be ingested by notification-service via Kafka
        try
        {
            await _fixture.Notifications.WaitForNotificationAsync(providerToken,
                n => n.BookingId == bookingId && n.EventType == "review.submitted",
                timeout: TimeSpan.FromSeconds(15));
        }
        catch (Exception ex)
        {
            var outbox = await BookingDbHelper.GetLatestOutboxAsync();
            var failed = await BookingDbHelper.GetNotificationDebugAsync();
            throw new InvalidOperationException($"Notification wait failed: {ex.Message}\nOutbox:\n{outbox}\nFailedEvents:\n{failed}");
        }

        _fixture.Shell.LoginAs(TestConfig.ProviderEmail, TestConfig.ProviderPassword);

        // Provider bell should show notification for the review (via Kafka consumer)
        Eventually.Assert(() => _fixture.Shell.ReadBellBadgeCount() > 0,
            timeout: TimeSpan.FromSeconds(30),
            message: "Provider bell badge should display at least 1 unread notification.");

        _fixture.Shell.OpenNotifications();
        Eventually.Assert(() =>
        {
            var panel = _fixture.Driver.FindElements(By.CssSelector(".cq-notifications"));
            return panel.Count > 0 && panel[0].Text.Contains("New review", StringComparison.OrdinalIgnoreCase);
        });

        var notifications = _fixture.Driver.FindElement(By.CssSelector(".cq-notifications"));
        Assert.Contains("New review", notifications.Text, StringComparison.OrdinalIgnoreCase);
    }

    private void SelectStarRating(int rating)
    {
        var input = _fixture.Driver.FindElement(By.CssSelector($".cq-star-picker input[value='{rating}']"));
        ((IJavaScriptExecutor)_fixture.Driver).ExecuteScript("arguments[0].click();", input);
    }

    private void FilterReviewsByRating(string rating)
    {
        var select = _fixture.Driver.FindElement(By.XPath("//section[contains(@class,'cq-reviews')]//select"));
        ((IJavaScriptExecutor)_fixture.Driver).ExecuteScript(@"
            arguments[0].value = arguments[1];
            arguments[0].dispatchEvent(new Event('change', { bubbles: true }));
        ", select, rating);
    }

    private IWebElement FindExploreCard(string title) =>
        _fixture.Driver.FindElement(By.XPath($"//div[contains(@class,'vd-service-card')][.//h3[contains(normalize-space(.), {XPathLiteral(title)})]]"));
}
