using System.Text.Json;
using CeylonQuest.E2E.Tests.Admin.Infrastructure;
using CeylonQuest.E2E.Tests.Infrastructure;
using OpenQA.Selenium;
using Xunit;

namespace CeylonQuest.E2E.Tests.Admin.Pages;

internal sealed class AdminCatalogListingsPage(AdminUi ui, AdminDashboardPage dashboard)
{
    public const string Path = "/api/catalog/reports/admin-listings";
    private const string Scope = ".ad-listings-table";
    public JsonElement[] Records { get; private set; } = [];
    public static string Key(JsonElement item) => item.Text("bookingType") + ":" + item.Text("id");
    public static string Status(JsonElement item) => item.Flag("isRemoved") || item.Text("status") == "Removed"
        ? "Removed" : item.Flag("isActive") ? "Active" : "Inactive";

    public void Open()
    {
        var baseline = ui.Sequence;
        dashboard.Open("listings", "Listing Management");
        Load(ui.Wait(Path, baseline).Body);
        AssertRows(Records);
    }

    private void Load(JsonElement body)
    {
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Records = body.EnumerateArray().Select(item => item.Clone()).ToArray();
        Assert.Equal(Records.Length, Records.Select(Key).Distinct().Count());
        foreach (var item in Records)
        {
            Assert.True(Guid.TryParse(item.Text("id"), out var id) && id != Guid.Empty);
            Assert.True(Guid.TryParse(item.Text("providerId"), out var providerId) && providerId != Guid.Empty);
            Assert.Contains(item.Text("bookingType"), new[] { "Experience", "Restaurant", "Accommodation" });
        }
        ui.Browser.Step($"Admin catalog listing response loaded: {Records.Length} listings.");
    }

    public void Refresh()
    {
        var baseline = ui.Sequence;
        ui.Button("Refresh", "//div[contains(@class,'ad-listing-filters')]");
        Load(ui.Wait(Path, baseline).Body);
        AssertRows(Records);
    }

    public void Reset()
    {
        ui.Browser.Fill(By.Id("admin-listing-search"), "");
        ui.Select("admin-listing-category", "all");
        ui.Select("admin-listing-status", "all");
        AssertRows(Records);
    }

    private static bool MatchesRow(string[] row, JsonElement item) => row.Length == 5 &&
        AdminUi.SameText(item.Text("serviceName") + " " + item.Text("id"), row[0]) &&
        AdminUi.SameText(item.Text("providerName"), row[1]) &&
        AdminUi.SameText(item.Text("bookingType"), row[2], ignoreCase: true) &&
        AdminUi.SameText(Status(item), row[3], ignoreCase: true);

    public void AssertRows(IReadOnlyList<JsonElement> expected)
    {
        ui.Browser.Until(_ => {
            var actual = ui.Table(Scope);
            return actual.Length == expected.Count && actual.Select((row, index) => MatchesRow(row, expected[index])).All(match => match);
        }, "Admin listing rows did not match the expected catalog identities/providers/categories/statuses.");
        if (expected.Count == 0)
            AdminUi.EqualText("No listings found", ui.Browser.Visible(By.CssSelector(".ad-empty__title")).Text);
        else
        {
            var rows = ui.Rows(Scope);
            for (var index = 0; index < expected.Count; index++)
            {
                var actions = rows[index].FindElements(By.TagName("button"));
                if (Status(expected[index]) == "Active")
                {
                    var button = Assert.Single(actions);
                    AdminUi.EqualText("Remove Listing", button.Text);
                    Assert.True(button.Enabled);
                }
                else Assert.Empty(actions);
            }
        }
        Assert.DoesNotContain(ui.Browser.Driver.FindElements(By.CssSelector("p[role='alert']")), element => element.Displayed);
    }

    public JsonElement Choose(bool active = false)
    {
        var id = AdminSettings.OptionalId("CQ_E2E_ADMIN_CATALOG_LISTING_ID");
        var type = TestSettings.Optional("CQ_E2E_ADMIN_CATALOG_LISTING_TYPE");
        var candidates = Records.Where(item =>
            (id is null || item.Text("id").Equals(id, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(type) || item.Text("bookingType").Equals(type, StringComparison.OrdinalIgnoreCase)) &&
            (!active || Status(item) == "Active")).ToArray();
        Assert.True(candidates.Length > 0, active ? "Removal cancellation requires an existing Active catalog listing."
            : "This positive listing search requires an existing catalog listing.");
        if (id is not null) Assert.Single(candidates);
        ui.Browser.Step("Selected listing key: " + Key(candidates[0]));
        return candidates[0];
    }

    public void SearchAndClear(string property)
    {
        var item = Choose();
        var query = item.Text(property).ToUpperInvariant();
        Assert.False(string.IsNullOrWhiteSpace(query));
        ui.Browser.Fill(By.Id("admin-listing-search"), query);
        var expected = Records.Where(record => new[] { "serviceName", "providerName", "id" }
            .Any(key => record.Text(key).Contains(query, StringComparison.OrdinalIgnoreCase))).ToArray();
        Assert.Contains(expected, record => Key(record) == Key(item));
        AssertRows(expected);
        ui.Browser.Fill(By.Id("admin-listing-search"), "");
        AssertRows(Records);
    }

    public void Category(string category)
    {
        ui.Select("admin-listing-category", category);
        AssertRows(Records.Where(item => item.Text("bookingType") == category).ToArray());
    }

    public void FilterStatus(string status)
    {
        ui.Select("admin-listing-status", status);
        AssertRows(status == "all" ? Records : Records.Where(item => Status(item) == status).ToArray());
    }

    public void CombinedFilters()
    {
        var item = Choose();
        ui.Select("admin-listing-category", item.Text("bookingType"));
        ui.Select("admin-listing-status", Status(item));
        var query = item.Text("id").ToUpperInvariant();
        ui.Browser.Fill(By.Id("admin-listing-search"), query);
        var expected = Records.Where(record => record.Text("bookingType") == item.Text("bookingType") &&
            Status(record) == Status(item) && new[] { "serviceName", "providerName", "id" }
                .Any(key => record.Text(key).Contains(query, StringComparison.OrdinalIgnoreCase))).ToArray();
        Assert.Contains(expected, record => Key(record) == Key(item));
        AssertRows(expected);
        Reset();
    }

    private IWebElement Row(JsonElement item)
    {
        var index = Array.FindIndex(Records, record => Key(record) == Key(item));
        Assert.True(index >= 0);
        return ui.Rows(Scope)[index];
    }

    public void AssertSameSnapshot(IReadOnlyList<JsonElement> original)
    {
        Assert.Equal(original.Count, Records.Length);
        foreach (var before in original)
        {
            var after = Assert.Single(Records, item => Key(item) == Key(before));
            foreach (var field in new[] { "serviceName", "providerName", "providerId", "bookingType" })
                Assert.Equal(before.Text(field), after.Text(field));
            Assert.Equal(Status(before), Status(after));
            Assert.Equal(before.Flag("isActive"), after.Flag("isActive"));
            Assert.Equal(before.Flag("isRemoved"), after.Flag("isRemoved"));
        }
    }

    public void CancelRemoval(JsonElement item)
    {
        Reset();
        var original = Records.ToArray();
        Row(item).FindElement(By.TagName("button")).Click();
        var sectionSelector = By.CssSelector("section[aria-label='Listing removal details']");
        var section = ui.Browser.Visible(sectionSelector);
        AdminUi.EqualText("Remove " + item.Text("serviceName"), section.FindElement(By.TagName("h2")).Text);
        Assert.Contains(item.Text("providerName"), section.Text);
        var reason = "Valid administrative review reason.\nCanceled without removing this listing.";
        ui.Browser.Fill(By.Id("listing-removal-reason"), reason);
        ui.InputValue(By.Id("listing-removal-reason"), reason);
        ui.Button("Continue to confirmation", "//section[@aria-label='Listing removal details']");
        var confirm = ui.Browser.Visible(By.CssSelector(".cq-confirm-modal"));
        AdminUi.EqualText("Remove Listing", confirm.FindElement(By.CssSelector(".cq-confirm-title")).Text);
        Assert.Contains(item.Text("serviceName"), confirm.FindElement(By.CssSelector(".cq-confirm-message")).Text);
        ui.Browser.Click(By.CssSelector(".cq-confirm-btn--cancel"));
        ui.Browser.Until(driver => !driver.FindElements(By.CssSelector(".cq-confirm-modal")).Any(element => element.Displayed),
            "Removal confirmation did not close after Cancel.");
        ui.InputValue(By.Id("listing-removal-reason"), reason);
        ui.Button("Cancel", "//section[@aria-label='Listing removal details']");
        ui.Browser.Until(driver => !driver.FindElements(sectionSelector).Any(element => element.Displayed), "Removal details did not close.");
        ui.NoWrites();
        Refresh(); // Real UI GET, not a direct setup or database request.
        AssertSameSnapshot(original);
        Assert.Equal("Active", Status(Assert.Single(Records, record => Key(record) == Key(item))));
    }
}
