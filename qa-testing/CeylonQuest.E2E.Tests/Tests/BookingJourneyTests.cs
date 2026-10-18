using CeylonQuest.E2E.Tests.Infrastructure;
using CeylonQuest.E2E.Tests.Pages;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Tests;

public sealed class BookingJourneyTests(ITestOutputHelper output)
{
    [BookingFact]
    [Trait("Category", "Booking")]
    public void VisitorBooking_ConfirmsPaymentBookingAndNotification_ManualStripeStep()
    {
        TestSettings.ValidateStay();
        using var browser = new BrowserSession(nameof(VisitorBooking_ConfirmsPaymentBookingAndNotification_ManualStripeStep), output);
        browser.Run(() => {
            var startedAt = DateTimeOffset.UtcNow;
            new LoginPage(browser).LoginVisitor();
            var modal = new ExplorePage(browser).OpenBooking();
            modal.PrepareValidStay();
            browser.Capture("ready-to-book");
            browser.Step("Submitting ONE booking through Book Stay; no automatic booking retries or cleanup.");
            browser.Click(BookingModal.Confirm);
            var shortId = new PaymentPage(browser).WaitForManualTestPayment();
            var bookingId = new MyBookingsPage(browser).VerifyNewConfirmedBooking(shortId, startedAt);
            new NotificationsPage(browser).VerifyBookingNotification(bookingId);
        });
    }
}
