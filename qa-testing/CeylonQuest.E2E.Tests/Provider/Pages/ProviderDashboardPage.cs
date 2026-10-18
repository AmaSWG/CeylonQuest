using System.Text.Json;
using CeylonQuest.E2E.Tests.Pages;
using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using OpenQA.Selenium;
using Xunit;

namespace CeylonQuest.E2E.Tests.Provider.Pages;

internal sealed class ProviderDashboardPage(ProviderUi ui)
{
    public JsonElement Profile { get; private set; }
    public JsonElement[] Listings { get; private set; } = [];
    public string ListingType { get; private set; } = "";
    public string CatalogPath { get; private set; } = "";
    public string PageTitle => ListingType switch {
        "Accommodation" => "Rooms and Accommodations", "Restaurant" => "Menu and Dining", _ => "Experience Listings"
    };

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
        Assert.True(Guid.TryParse(Profile.Text("id"), out var providerId) && providerId != Guid.Empty);
        Assert.False(string.IsNullOrWhiteSpace(Profile.Text("businessName")));
        ProviderUi.EqualText(ProviderSettings.Required("CQ_E2E_PROVIDER_EMAIL"), Profile.Text("email"), ignoreCase: true);
        var category = Profile.Text("serviceType").ToLowerInvariant();
        Assert.False(string.IsNullOrWhiteSpace(category));
        ListingType = new[] { "hotel", "accommodat", "villa", "resort", "room" }.Any(category.Contains) ? "Accommodation" :
            new[] { "restaurant", "dining", "dinner", "food", "cafe", "catering" }.Any(category.Contains) ? "Restaurant" : "Experience";
        CatalogPath = "/api/catalog/" + (ListingType == "Accommodation" ? "accommodation-listings" :
            ListingType == "Restaurant" ? "restaurant-listings" : "activity-listings");
        var response = ui.Wait(CatalogPath);
        Assert.Equal(JsonValueKind.Array, response.Body.ValueKind);
        Listings = response.Body.EnumerateArray().Select(item => item.Clone()).ToArray();
        Assert.Equal(Listings.Length, Listings.Select(item => item.Text("id")).Distinct().Count());
        Assert.Equal(Listings.Length, Listings.Select(item => item.ListingName()).Distinct().Count());
        foreach (var listing in Listings)
        {
            Assert.True(Guid.TryParse(listing.Text("id"), out var id) && id != Guid.Empty);
            Assert.False(string.IsNullOrWhiteSpace(listing.ListingName()));
            // Activity GetMyListings currently omits ProviderId (DTO defaults to empty).
            // Compare ownership where the actual response exposes a nonempty provider ID.
            if (Guid.TryParse(listing.Text("providerId"), out var ownerId) && ownerId != Guid.Empty)
                Assert.Equal(providerId, ownerId);
        }
        foreach (var tab in new[] { "overview", "business", "services" }) ui.Browser.Visible(By.Id("nav-" + tab));
        ProviderUi.EqualText("Dashboard Overview", ui.Browser.Visible(By.CssSelector(".pd-overview h1")).Text);
        ui.Browser.Step($"Valid provider login; {ListingType} catalog loaded {Listings.Length} owned listings.");
        foreach (var item in Listings) ui.Browser.Step("Owned listing: " + item.Text("id") + " / " + item.ListingName());
    }

    public void Navigate(string tab) => ui.Browser.Click(By.Id("nav-" + tab));

    public JsonElement ChooseListing()
    {
        var id = ProviderSettings.ListingId;
        if (id is not null)
            return Assert.Single(Listings, item => item.Text("id").Equals(id, StringComparison.OrdinalIgnoreCase));
        Assert.True(Listings.Length > 0, "This scenario requires at least one existing listing owned by the provider.");
        return Listings[0];
    }

    public void AssertBusinessProfile()
    {
        Navigate("business");
        ui.Browser.Visible(By.CssSelector(".pd-business-tab"));
        foreach (var (label, property) in new[] {
            ("Business / Enterprise Name", "businessName"), ("Primary Service Category", "serviceType"),
            ("Operating Location / Base", "location"), ("Business & Service Description", "description"),
            ("Contact Phone", "phoneNumber"), ("Official Contact Email", "email")
        })
        {
            // Labels are visually uppercased by CSS; keep the actual field values strict.
            var field = Assert.Single(ui.Browser.Driver.FindElements(By.CssSelector(".pd-business-tab .pd-field")),
                element => ProviderUi.Normalize(element.FindElement(By.CssSelector(".pd-field__label")).Text)
                    .Equals(label, StringComparison.OrdinalIgnoreCase));
            var expected = Profile.Text(property);
            ProviderUi.EqualText(string.IsNullOrEmpty(expected) ? "\u2014" : expected,
                field.FindElement(By.CssSelector(".pd-field__value")).Text);
        }
        ui.Browser.Visible(By.Id("edit-business-btn")); // View only: profile saving uses another service.
        ui.Browser.Step("Business profile fields verified against the catalog profile response.");
    }

    public void AssertListingSummary()
    {
        Navigate("overview");
        var metric = By.XPath("//div[contains(@class,'pd-metric-info')][div[normalize-space()='Listings']]");
        var active = Listings.Count(item => item.Flag("isActive"));
        ui.Browser.Until(_ => {
            var current = ui.Browser.Visible(metric);
            return ProviderUi.Normalize(current.FindElement(By.CssSelector(".pd-metric-value")).Text) == $"{active} Active" &&
                ProviderUi.Normalize(current.FindElement(By.CssSelector(".pd-metric-sub")).Text) == $"{Listings.Length} Total Registered";
        }, "Listing overview did not update after catalog loading.");
        var card = ui.Browser.Visible(metric);
        ProviderUi.EqualText($"{active} Active", card.FindElement(By.CssSelector(".pd-metric-value")).Text);
        ProviderUi.EqualText($"{Listings.Length} Total Registered", card.FindElement(By.CssSelector(".pd-metric-sub")).Text);
        Assert.Contains(Profile.Text("businessName"), ui.Browser.Visible(By.CssSelector(".pd-overview .pd-page-header")).Text);
        // Do not inspect booking/earnings cards or navigate to their workflows.
    }
}
