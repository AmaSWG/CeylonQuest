using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using CeylonQuest.E2E.Tests.Provider.Pages;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Provider.Tests;

[Trait("Workflow", "Provider")]
[Trait("Safety", "ReadOnly")]
public sealed class ProviderDashboardTests(ITestOutputHelper output) : ProviderTestBase(output)
{
    [ProviderFact]
    [Trait("CaseId", "PROV-AUTH-01")]
    public void ProviderLogin_DisplaysRoleAndNavigation() => Run((ui, dashboard) => {
        // ProviderTestBase verifies the real login, stored role, all provider nav IDs,
        // profile/catalog HTTP success, and Dashboard Overview heading.
        Assert.Equal("overview", ui.Browser.Driver.FindElements(By.CssSelector(".cq-nav-btn.active"))
            .Single().GetDomAttribute("id")!.Replace("nav-", ""));
    });

    [ProviderFact]
    [Trait("CaseId", "PROV-DASH-01")]
    public void Overview_DisplaysLoadedStatisticsAndSections() => Run((ui, dashboard) => {
        var bookings = new ProviderBookingsPage(ui, dashboard);
        bookings.Open(); // The existing app only loads bookings on the Bookings tab.
        dashboard.AssertOverview(bookings.Records);
    });

    [ProviderFact]
    [Trait("CaseId", "PROV-DASH-02")]
    public void OverviewQuickActions_OpenManagementTabs() => Run((ui, dashboard) => {
        ui.ClickText("//div[contains(@class,'pd-quick-actions')]", "Add New Listing");
        ui.Browser.Visible(By.Id("add-listing-btn"));
        Assert.Contains("active", ui.Browser.Visible(By.Id("nav-services")).GetDomAttribute("class"));
        dashboard.Navigate("overview");
        ui.ClickText("//div[contains(@class,'pd-quick-actions')]", "Account Settings");
        Assert.Contains("active", ui.Browser.Visible(By.Id("nav-account")).GetDomAttribute("class"));
        ProviderUi.EqualText("Account & Security", ui.Browser.Visible(By.CssSelector(".pd-account-view h1")).Text);
    });

    [ProviderFact]
    [Trait("CaseId", "PROV-AUTH-02")]
    public void Logout_RemovesSessionAndProtectedDashboard() => Run((ui, dashboard) =>
        dashboard.LogoutAndAssertProtectedDashboardGone());
}
