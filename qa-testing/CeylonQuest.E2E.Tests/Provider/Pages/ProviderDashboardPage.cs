using System.Text.Json;
using CeylonQuest.E2E.Tests.Infrastructure;
using CeylonQuest.E2E.Tests.Pages;
using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using OpenQA.Selenium;
using Xunit;

namespace CeylonQuest.E2E.Tests.Provider.Pages;

internal sealed class ProviderDashboardPage(ProviderUi ui)
{
    public static readonly string[] Navigation = ["overview", "business", "services", "bookings", "reviews", "notifications", "platform-reviews", "account"];
    public JsonElement Profile { get; private set; }
    public JsonElement[] Listings { get; private set; } = [];
    public string ListingType { get; private set; } = "";
    public string CatalogPath { get; private set; } = "";
    public string CatalogOriginUrl { get; private set; } = "";

    public void Login()
    {
        var login = new LoginPage(ui.Browser);
        login.OpenFromHome();
        ui.Observe();
        login.Submit(ProviderSettings.Required("CQ_E2E_PROVIDER_EMAIL"), ProviderSettings.Required("CQ_E2E_PROVIDER_PASSWORD"));
        ui.Browser.Visible(By.Id("nav-overview"));
        Assert.Equal("Provider", ui.Browser.Script.ExecuteScript("return localStorage.getItem('userRole')"));
        Assert.True(ui.Browser.Script.ExecuteScript("return Boolean(localStorage.getItem('authToken'))") is true);
        Profile = ui.Wait("/api/catalog/provider/profile").Body;
        var category = Profile.Text("serviceType").ToLowerInvariant();
        ListingType = new[] { "hotel", "accommodat", "villa", "resort", "room" }.Any(category.Contains) ? "Accommodation" :
            new[] { "restaurant", "dining", "dinner", "food", "cafe", "catering" }.Any(category.Contains) ? "Restaurant" : "Experience";
        CatalogPath = "/api/catalog/" + (ListingType == "Accommodation" ? "accommodation-listings" :
            ListingType == "Restaurant" ? "restaurant-listings" : "activity-listings");
        RefreshListingSnapshot();
        foreach (var key in Navigation) ui.Browser.Visible(By.Id("nav-" + key));
        ProviderUi.EqualText("Dashboard Overview", ui.Browser.Visible(By.CssSelector(".pd-overview h1")).Text);
        ui.Browser.Step("Provider login and actual role-specific navigation verified; category: " + ListingType);
        foreach (var listing in Listings)
            ui.Browser.Step("Provider-owned listing ID: " + listing.Text("id") + " / " + listing.ListingName());
    }

    public void RefreshListingSnapshot(int after = -1)
    {
        var response = ui.Wait(CatalogPath, after);
        Assert.Equal(JsonValueKind.Array, response.Body.ValueKind);
        CatalogOriginUrl = response.Url;
        Listings = response.Body.EnumerateArray().ToArray();
    }

    public void Navigate(string tab) => ui.Browser.Click(By.Id("nav-" + tab));

    public void AssertOverview(JsonElement[] bookings)
    {
        Navigate("overview");
        ui.Browser.Visible(By.CssSelector(".pd-overview"));
        foreach (var heading in new[] { "Business Profile Summary", "Recent Bookings", "Recent Notifications" })
            ui.Browser.Visible(By.XPath($"//div[contains(@class,'pd-overview')]//h2[normalize-space()={ProviderUi.Literal(heading)}]"));
        var active = Listings.Count(item => !item.TryGetProperty("isActive", out var status) || status.GetBoolean());
        ui.Browser.Until(_ => ProviderUi.Normalize(Metric("Listings")) == $"{active} Active" &&
            ProviderUi.Normalize(Metric("Total Bookings")) == bookings.Length.ToString(),
            "Overview statistics did not update after their real data loaded.");
        ProviderUi.EqualText($"{active} Active", Metric("Listings"));
        ProviderUi.EqualText(bookings.Length.ToString(), Metric("Total Bookings"));
        var earnings = bookings.Where(item => item.Text("status") is "Confirmed" or "Completed").Sum(item => item.Money("totalAmount"));
        ProviderUi.EqualText(ProviderUi.BrowserMoney(ui.Browser, earnings, 0), Metric("Estimated Earnings"));
        if (!string.IsNullOrWhiteSpace(Profile.Text("businessName")))
            Assert.Contains(Profile.Text("businessName"), ui.Browser.Visible(By.CssSelector(".pd-overview .pd-page-header")).Text);
        Assert.Equal(3, ui.Browser.Driver.FindElements(By.CssSelector(".pd-metrics-grid .pd-metric-card")).Count);
    }

    private string Metric(string title) => ui.Browser.Visible(By.XPath(
        $"//div[contains(@class,'pd-metric-info')][div[normalize-space()={ProviderUi.Literal(title)}]]/div[contains(@class,'pd-metric-value')]")).Text;

    public void LogoutAndAssertProtectedDashboardGone()
    {
        ui.Browser.Click(By.Id("pd-logout-btn"));
        ui.Browser.Visible(By.Id("nav-login-btn"));
        Assert.True(ui.Browser.Script.ExecuteScript("return !localStorage.getItem('authToken') && !localStorage.getItem('userRole')") is true);
        ui.Browser.Driver.Navigate().Refresh();
        ui.Browser.Visible(By.Id("nav-login-btn"));
        ui.Browser.Driver.Navigate().GoToUrl(TestSettings.BaseUrl);
        ui.Browser.Visible(By.Id("nav-login-btn"));
        foreach (var key in Navigation) Assert.Empty(ui.Browser.Driver.FindElements(By.Id("nav-" + key)));
        // Dashboard tabs have no independent provider URL. Reload/reopen checks the
        // implemented UI auth boundary without inventing /provider/dashboard routes.
    }
}
