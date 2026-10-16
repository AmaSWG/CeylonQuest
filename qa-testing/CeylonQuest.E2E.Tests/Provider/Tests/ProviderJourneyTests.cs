using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using CeylonQuest.E2E.Tests.Provider.Pages;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Provider.Tests;

[Trait("Workflow", "Provider")]
[Trait("Safety", "ReadOnly")]
public sealed class ProviderJourneyTests(ITestOutputHelper output) : ProviderTestBase(output)
{
    [ProviderFact("CQ_E2E_PROVIDER_LISTING_ID", "CQ_E2E_PROVIDER_BOOKING_ID", "CQ_E2E_PROVIDER_REVIEW_LISTING_A", "CQ_E2E_PROVIDER_NOTIFICATION_ID")]
    [Trait("CaseId", "PROV-E2E-01")]
    public void ProviderJourney_LoginOverviewListingsBookingsReviewsNotificationsLogout() => Run((ui, dashboard) => {
        var bookings = new ProviderBookingsPage(ui, dashboard);
        bookings.Open();
        dashboard.AssertOverview(bookings.Records);
        ui.Browser.Capture("journey-overview");
        var listings = new ProviderListingsPage(ui, dashboard);
        listings.Open();
        listings.AssertOwnedListing(ProviderSettings.Id("CQ_E2E_PROVIDER_LISTING_ID"));
        ui.Browser.Capture("journey-owned-listing");
        bookings.Open();
        bookings.AssertDetails(ProviderSettings.Id("CQ_E2E_PROVIDER_BOOKING_ID"));
        var reviews = new ProviderReviewsPage(ui, dashboard);
        reviews.Open();
        reviews.Select(ProviderSettings.Id("CQ_E2E_PROVIDER_REVIEW_LISTING_A"));
        reviews.AssertReviews(true);
        ui.Browser.Capture("journey-customer-reviews");
        var notifications = new ProviderNotificationsPage(ui, dashboard);
        notifications.Open();
        notifications.AssertConfiguredNotification(ProviderSettings.Id("CQ_E2E_PROVIDER_NOTIFICATION_ID"));
        ui.Browser.Capture("journey-notification");
        dashboard.LogoutAndAssertProtectedDashboardGone();
    });
}
