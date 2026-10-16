using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using CeylonQuest.E2E.Tests.Provider.Pages;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Provider.Tests;

[Trait("Workflow", "Provider")]
public sealed class ProviderNotificationTests(ITestOutputHelper output) : ProviderTestBase(output)
{
    [ProviderFact]
    [Trait("Safety", "ReadOnly")]
    [Trait("CaseId", "PROV-NOTIF-01")]
    public void Notifications_LoadCardsOrGenuineEmptyState() => Run((ui, dashboard) => {
        var notifications = new ProviderNotificationsPage(ui, dashboard);
        notifications.Open();
        notifications.AssertVisibleCards(false);
    });

    [ProviderFact("CQ_E2E_PROVIDER_NOTIFICATION_ID")]
    [Trait("Safety", "ReadOnly")]
    [Trait("CaseId", "PROV-NOTIF-02")]
    public void ConfiguredNotification_DisplaysItsTitleMessageTimestampAndReadState() => Run((ui, dashboard) => {
        var notifications = new ProviderNotificationsPage(ui, dashboard);
        notifications.Open();
        notifications.AssertConfiguredNotification(ProviderSettings.Id("CQ_E2E_PROVIDER_NOTIFICATION_ID"));
    });

    [ProviderWriteFact("CQ_E2E_PROVIDER_NOTIFICATION_ID")]
    [Trait("Safety", "Writes")]
    [Trait("CaseId", "PROV-NOTIF-03")]
    public void DedicatedUnreadNotification_MarkReadPersists() => Run((ui, dashboard) => {
        var notifications = new ProviderNotificationsPage(ui, dashboard);
        notifications.Open();
        notifications.MarkRead(ProviderSettings.Id("CQ_E2E_PROVIDER_NOTIFICATION_ID"));
    }, allowsWrites: true);
}
