using System.Globalization;
using System.Text.Json;
using CeylonQuest.E2E.Tests.Infrastructure;
using OpenQA.Selenium;
using Xunit;

namespace CeylonQuest.E2E.Tests.Pages;

internal sealed class PaymentPage(BrowserSession browser)
{
    public string WaitForManualTestPayment()
    {
        browser.Until(d => Uri.TryCreate(d.Url, UriKind.Absolute, out var url) && url.Host == "checkout.stripe.com",
            "Booking did not redirect to Stripe Checkout. Check booking/checkout errors; do not retry by creating another booking.");
        // Stripe sessions identify test/live mode in the hosted URL. Refuse live or
        // unknown sessions even when the operator configuration claims test mode.
        Assert.Contains("cs_test_", browser.Driver.Url, StringComparison.Ordinal);
        browser.Step("MANUAL STEP: Complete this visible Stripe TEST checkout with an approved test card. Selenium will resume on return. Do not use real card details.");
        browser.Until(d => Uri.TryCreate(d.Url, UriKind.Absolute, out var url) &&
            url.GetLeftPart(UriPartial.Authority) == new Uri(TestSettings.BaseUrl).GetLeftPart(UriPartial.Authority) &&
            url.AbsolutePath.StartsWith("/payment/success", StringComparison.OrdinalIgnoreCase),
            "Test checkout was not completed/returned to /payment/success within the manual payment timeout.",
            TestSettings.Seconds("CQ_E2E_PAYMENT_WAIT_SECONDS", 240));
        browser.Until(d => d.FindElements(By.CssSelector(".psp-success-title")).Any(e => e.Displayed),
            "The application did not confirm payment. A pending/received message is not successful confirmation.");
        Assert.Equal("Payment Successful", browser.Visible(By.CssSelector(".psp-success-title")).Text);
        Assert.Equal("Confirmed", browser.Visible(By.CssSelector(".psp-badge--confirmed")).Text);
        Assert.Equal("Paid", browser.Visible(By.CssSelector(".psp-badge--paid")).Text);
        var shortId = browser.Visible(By.XPath("//div[contains(@class,'psp-detail-row')][span[normalize-space()='Booking ID']]/strong")).Text.TrimStart('#');
        Assert.Matches("^[A-Fa-f0-9]{8}$", shortId);
        browser.Capture("confirmed-payment");
        return shortId;
    }
}

internal sealed class MyBookingsPage(BrowserSession browser)
{
    public string VerifyNewConfirmedBooking(string shortId, DateTimeOffset startedAt)
    {
        browser.ObserveUiReads();
        browser.Click(By.CssSelector(".psp-card .psp-btn--primary"));
        browser.Visible(By.CssSelector(".vb-page"));
        JsonElement[] records = [];
        browser.Until(_ => {
            var payload = browser.ReadUiPayload("/api/user-bookings/my");
            if (payload is not { ValueKind: JsonValueKind.Array }) return false;
            records = payload.Value.EnumerateArray().ToArray();
            return records.Any(e => e.GetProperty("id").GetString()!.StartsWith(shortId, StringComparison.OrdinalIgnoreCase));
        }, "My Bookings did not return the booking ID shown on the payment confirmation.");
        var booking = Assert.Single(records, e => e.GetProperty("id").GetString()!
            .StartsWith(shortId, StringComparison.OrdinalIgnoreCase));
        var id = booking.GetProperty("id").GetString()!;
        Assert.Equal("Accommodation Booking", booking.GetProperty("bookingType").GetString());
        Assert.Equal(TestSettings.Required("CQ_E2E_ACCOMMODATION_TITLE"), booking.GetProperty("serviceName").GetString());
        Assert.Equal("Confirmed", booking.GetProperty("status").GetString());
        Assert.Equal("Paid", booking.GetProperty("paymentStatus").GetString());
        Assert.Equal(TestSettings.Required("CQ_E2E_CHECK_IN"), booking.GetProperty("checkInDate").GetString());
        Assert.Equal(TestSettings.Required("CQ_E2E_CHECK_OUT"), booking.GetProperty("checkOutDate").GetString());
        Assert.Equal(TestSettings.Guests, booking.GetProperty("peopleCount").GetInt32());
        var created = DateTimeOffset.Parse(booking.GetProperty("createdAt").GetString()!, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal);
        Assert.True(created >= startedAt.AddSeconds(-5), "An older booking cannot satisfy this journey.");

        // With the default unfiltered All tab, frontend .filter preserves response order.
        // Verify the correlated row plus its actual modal, not just the observed JSON.
        var index = Array.FindIndex(records, e => e.GetProperty("id").GetString() == id);
        var row = browser.Until(d => {
            var rows = d.FindElements(By.CssSelector(".vb-table tbody tr"));
            return rows.Count == records.Length ? rows[index] : null!;
        }, "The booking table did not render the returned bookings.");
        Assert.Contains(TestSettings.Required("CQ_E2E_ACCOMMODATION_TITLE"), row.Text);
        Assert.Contains("Payment: Paid", row.Text);
        Assert.Contains("Confirmed", row.Text);
        row.FindElement(By.CssSelector(".vb-view-btn")).Click();
        Assert.Equal(TestSettings.Required("CQ_E2E_ACCOMMODATION_TITLE"), browser.Visible(By.CssSelector(".vb-modal__header h2")).Text);
        Assert.Equal("Confirmed", browser.Visible(By.CssSelector(".vb-modal-status")).Text.Split('\n').Last().Trim());
        Assert.Equal(FormattedDate(TestSettings.Date("CQ_E2E_CHECK_IN")), Detail("Check-in"));
        Assert.Equal(FormattedDate(TestSettings.Date("CQ_E2E_CHECK_OUT")), Detail("Check-out"));
        Assert.Equal(TestSettings.Guests.ToString(CultureInfo.InvariantCulture), Detail("Guests"));
        var sriLankaCreated = TimeZoneInfo.ConvertTime(created, TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo"));
        Assert.Equal(sriLankaCreated.ToString("MMM d, yyyy | h:mm tt", CultureInfo.GetCultureInfo("en-US")), Detail("Created At"));
        browser.Capture("my-booking-details");
        browser.Click(By.CssSelector(".vb-modal__close"));
        browser.Step("New confirmed, paid accommodation booking verified. Booking ID: " + id);
        return id;
    }

    private string Detail(string label) => browser.Visible(By.XPath(
        $"//div[contains(@class,'vb-detail-item')][span[normalize-space()='{label}']]/strong")).Text;
    private static string FormattedDate(DateOnly date) => date.ToString("MMM d, yyyy", CultureInfo.GetCultureInfo("en-US"));
}

internal sealed class NotificationsPage(BrowserSession browser)
{
    public void VerifyBookingNotification(string bookingId)
    {
        browser.ObserveUiReads();
        browser.Click(By.Id("nav-notifications"));
        browser.Visible(By.CssSelector("section[aria-label='Notifications']"));
        var scanningForward = true;
        // Delivery and the UI's own 30-second polling both need time to complete.
        // Errors and timeouts fail this journey; an unavailable service is never a pass.
        browser.Until(d => {
            var error = d.FindElements(By.CssSelector(".cq-notifications__error")).FirstOrDefault(e => e.Displayed);
            if (error is not null)
                throw new InvalidOperationException("Notification service unavailable in this environment: " + error.Text);
            var payload = browser.ReadUiPayload("/api/notifications");
            if (payload is null || !payload.Value.TryGetProperty("items", out var items)) return false;
            var pagination = d.FindElements(By.CssSelector(".cq-notifications__pagination span")).FirstOrDefault();
            if (pagination is not null && !pagination.Text.StartsWith(
                $"Page {payload.Value.GetProperty("page").GetInt32()} of ", StringComparison.Ordinal)) return false;
            var matches = items.EnumerateArray().Where(item =>
                item.GetProperty("bookingId").GetString() == bookingId &&
                item.GetProperty("eventType").GetString() is "booking.created" or "payment.completed").ToArray();
            foreach (var item in matches)
            {
                var title = item.GetProperty("title").GetString();
                var message = item.GetProperty("message").GetString();
                if (d.FindElements(By.CssSelector(".cq-notifications__item")).Any(card =>
                    card.Displayed && card.FindElement(By.TagName("h2")).Text == title &&
                    card.FindElement(By.TagName("p")).Text == message)) return true;
            }
            // Check older pages if the current response has no matching booking.
            // At the last page, return to page 1 to await the next delivery cycle.
            if (d.FindElements(By.CssSelector(".cq-notifications [role='status']")).Any(e => e.Displayed)) return false;
            var next = d.FindElements(By.XPath("//nav[@aria-label='Notification pages']/button[normalize-space()='Next']"))
                .FirstOrDefault(e => e.Enabled);
            var previous = d.FindElements(By.XPath("//nav[@aria-label='Notification pages']/button[normalize-space()='Previous']"))
                .FirstOrDefault(e => e.Enabled);
            if (scanningForward && next is null) scanningForward = false;
            if (!scanningForward && previous is null) scanningForward = true;
            var navigation = scanningForward ? next : previous;
            navigation?.Click();
            return false;
        }, "No rendered notification correlated to this booking before the timeout. Check notification deployment, Kafka delivery and polling.",
            TestSettings.Seconds("CQ_E2E_NOTIFICATION_WAIT_SECONDS", 100));
        browser.Capture("booking-notification");
        browser.Step("Rendered notification verified against the exact new Booking ID: " + bookingId);
    }
}
