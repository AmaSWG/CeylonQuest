using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using CeylonQuest.E2E.Tests.Provider.Pages;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Provider.Tests;

[Trait("Workflow", "Provider")]
public sealed class ProviderBookingTests(ITestOutputHelper output) : ProviderTestBase(output)
{
    [ProviderFact("CQ_E2E_PROVIDER_BOOKING_ID")]
    [Trait("Safety", "ReadOnly")]
    [Trait("CaseId", "PROV-BOOK-01")]
    public void ProviderBooking_DisplaysMatchingCustomerDetailsAndStatus() => Run((ui, dashboard) => {
        var bookings = new ProviderBookingsPage(ui, dashboard);
        bookings.Open();
        bookings.AssertDetails(ProviderSettings.Id("CQ_E2E_PROVIDER_BOOKING_ID"));
    });

    [ProviderFact("CQ_E2E_PROVIDER_BOOKING_ID")]
    [Trait("Safety", "ReadOnly")]
    [Trait("CaseId", "PROV-BOOK-02")]
    public void BookingSearchAndStatusFilter_ExcludeOtherStatuses() => Run((ui, dashboard) => {
        var bookings = new ProviderBookingsPage(ui, dashboard);
        bookings.Open();
        bookings.AssertStatusFilter(ProviderSettings.Id("CQ_E2E_PROVIDER_BOOKING_ID"));
    });

    [ProviderWriteFact("CQ_E2E_PROVIDER_ATTENDED_BOOKING_ID")]
    [Trait("Safety", "Writes")]
    [Trait("CaseId", "PROV-BOOK-03")]
    public void DedicatedPastBooking_VerifyAttended() => Run((ui, dashboard) => {
        var bookings = new ProviderBookingsPage(ui, dashboard);
        bookings.Open();
        bookings.VerifyAttendance(ProviderSettings.Id("CQ_E2E_PROVIDER_ATTENDED_BOOKING_ID"), "Attended");
    }, allowsWrites: true);

    [ProviderWriteFact("CQ_E2E_PROVIDER_NOSHOW_BOOKING_ID")]
    [Trait("Safety", "Writes")]
    [Trait("CaseId", "PROV-BOOK-04")]
    public void DedicatedPastBooking_MarkNoShow() => Run((ui, dashboard) => {
        var bookings = new ProviderBookingsPage(ui, dashboard);
        bookings.Open();
        bookings.VerifyAttendance(ProviderSettings.Id("CQ_E2E_PROVIDER_NOSHOW_BOOKING_ID"), "NoShow");
    }, allowsWrites: true);
}
