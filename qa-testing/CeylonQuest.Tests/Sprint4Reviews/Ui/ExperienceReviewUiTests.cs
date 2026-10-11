using System.Text.Json;
using CeylonQuest.Tests.Sprint4Notifications;
using CeylonQuest.Tests.Sprint4Notifications.Infrastructure;
using CeylonQuest.Tests.Sprint4Reviews.Infrastructure;
using OpenQA.Selenium;

namespace CeylonQuest.Tests.Sprint4Reviews.Ui;

/// <summary>
/// Story 12.1 "Review an Experience" - Selenium UI test cases (TC-12.1-UI-01..12).
///
/// IMPORTANT - test-data limitation found during investigation (see repo memory /
/// Sprint4Reviews investigation notes shared with the requester before this suite was written):
/// a booking only becomes reviewable ("Completed") after an owning Provider calls
/// PATCH /api/provider-bookings/{bookingType}/{bookingId}/attendance. Provider accounts cannot be
/// self-provisioned by this harness (registration needs real email OTP verification *and* manual
/// admin approval - same constraint the Story 11.1 suite documented in Sprint4Notifications/README.md).
/// Per the requester's explicit decision, every scenario that needs a Completed booking (or a
/// Provider login) is marked Skip below instead of faking a pass, and is reported as a gap.
/// Only Pending/Cancelled-booking and "no reviews yet" scenarios are exercised end-to-end here.
/// </summary>
[Collection(nameof(ReviewUiCollection))]
public sealed class ExperienceReviewUiTests
{
    private readonly ReviewUiFixture _fixture;

    public ExperienceReviewUiTests(ReviewUiFixture fixture) => _fixture = fixture;

    private const string SkipNoCompletedBooking =
        "No automatable path to a Completed booking in this environment: completing a booking " +
        "requires a logged-in Provider account to call the attendance endpoint, and provider " +
        "accounts cannot be self-provisioned (real email OTP + manual admin approval required).";

    private const string SkipNoReviewData =
        "The star filter UI is now built (ListingReviews.jsx has a 'Filter reviews by rating' select, " +
        "added after this suite's original investigation), but exercising it needs an experience with " +
        "multiple reviews across different ratings. Creating those reviews hits the same blocker as " +
        "UI-02/03/04/05/08/12: no automatable path to a Completed booking without a Provider login.";

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

        var response = await _fixture.Booking.CreateExperienceBookingAsync(visitor.Token, listing.Id, slot.Date, slot.TimeSlot, 1);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Booking creation failed with status {response.StatusCode}: {err}");
        }
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var bookingId = body.RootElement.GetProperty("id").GetGuid();

        return (bookingId, listing.Title);
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
        // The UI explicitly suppresses the ineligibility reason paragraph once status is Cancelled
        // (VisitorBookingsTab.jsx: `booking.status !== 'Cancelled'` guards the <p>).
    }

    [Fact(DisplayName = "TC-12.1-UI-02: review form validation", Skip = SkipNoCompletedBooking)]
    public void Ui02_ReviewFormValidation() { }

    [Fact(DisplayName = "TC-12.1-UI-03: S1 submit a review", Skip = SkipNoCompletedBooking)]
    public void Ui03_SubmitReview() { }

    [Fact(DisplayName = "TC-12.1-UI-04: S1/S5 rating and count update on the detail page", Skip = SkipNoCompletedBooking)]
    public void Ui04_RatingAndCountUpdate() { }

    [Fact(DisplayName = "TC-12.1-UI-05: S5 rating on the listing card", Skip = SkipNoCompletedBooking)]
    public void Ui05_RatingOnListingCard() { }

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

    [Fact(DisplayName = "TC-12.1-UI-08: S4 duplicate review", Skip = SkipNoCompletedBooking)]
    public void Ui08_DuplicateReview() { }

    [Fact(DisplayName = "TC-12.1-UI-09: S3 star filter", Skip = SkipNoReviewData)]
    public void Ui09_StarFilter() { }

    [Fact(DisplayName = "TC-12.1-UI-10: S3 filter with no matches and reset", Skip = SkipNoReviewData)]
    public void Ui10_StarFilterEmptyAndReset() { }

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
            throw new InvalidOperationException("No unreviewed Experience listing found - seed at least one active, approved experience with zero reviews to run this suite.");

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
        // Per TC-12.1-UI-11's expected result: no misleading average should be shown for an
        // unreviewed listing (no "0 / 5" / "NaN"). If this fails, ListingReviews.jsx's
        // unconditional `{data && <p>...average.toFixed(2)...}` summary line is the cause -
        // report as a possible defect rather than loosening this assertion.
        Assert.DoesNotContain("0.00 / 5", section.Text);
        Assert.DoesNotContain("NaN", section.Text);
    }

    [Fact(DisplayName = "TC-12.1-UI-12: S1 provider notification (cross-story with 11.1)", Skip = SkipNoCompletedBooking)]
    public void Ui12_ProviderNotification() { }

    private IWebElement FindExploreCard(string title) =>
        _fixture.Driver.FindElement(By.XPath($"//div[contains(@class,'vd-service-card')][.//h3[contains(normalize-space(.), {XPathLiteral(title)})]]"));
}
