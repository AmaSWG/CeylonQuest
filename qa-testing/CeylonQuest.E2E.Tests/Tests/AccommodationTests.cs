using CeylonQuest.E2E.Tests.Infrastructure;
using CeylonQuest.E2E.Tests.Pages;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Tests;

public sealed class AccommodationTests(ITestOutputHelper output)
{
    [AccommodationFact]
    [Trait("Category", "Search")]
    public void AccommodationSearch_OpensMatchingDetails()
    {
        using var browser = new BrowserSession(nameof(AccommodationSearch_OpensMatchingDetails), output);
        browser.Run(() => {
            new LoginPage(browser).LoginVisitor();
            new ExplorePage(browser).OpenAccommodationDetails();
            browser.Capture("accommodation-details");
        });
    }

    [StayFact]
    [Trait("Category", "Validation")]
    public void MissingCheckoutDate_DisablesBookingSubmission()
    {
        using var browser = new BrowserSession(nameof(MissingCheckoutDate_DisablesBookingSubmission), output);
        browser.Run(() => {
            new LoginPage(browser).LoginVisitor();
            var modal = new ExplorePage(browser).OpenBooking();
            modal.PrepareValidStay();
            browser.ClearDate(BookingModal.CheckOut);
            Assert.Equal("", browser.Visible(BookingModal.CheckOut).GetDomProperty("value"));
            modal.AssertSubmissionDisabled();
            browser.Capture("missing-checkout");
        });
    }

    [StayFact]
    [Trait("Category", "Validation")]
    public void CheckoutBeforeCheckin_DisablesBookingSubmission()
    {
        TestSettings.ValidateStay();
        using var browser = new BrowserSession(nameof(CheckoutBeforeCheckin_DisablesBookingSubmission), output);
        browser.Run(() => {
            new LoginPage(browser).LoginVisitor();
            var modal = new ExplorePage(browser).OpenBooking();
            // Establish a valid, available stay first so missing inventory cannot make
            // this date validation scenario pass for an unrelated reason.
            modal.PrepareValidStay();
            browser.SetDate(BookingModal.CheckOut, TestSettings.Date("CQ_E2E_CHECK_IN").AddDays(-1));
            modal.AssertSubmissionDisabled();
            browser.Capture("invalid-checkout");
        });
    }
}
