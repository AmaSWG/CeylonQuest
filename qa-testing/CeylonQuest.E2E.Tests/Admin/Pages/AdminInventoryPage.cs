using System.Globalization;
using System.Text.Json;
using CeylonQuest.E2E.Tests.Admin.Infrastructure;
using CeylonQuest.E2E.Tests.Infrastructure;
using OpenQA.Selenium;
using Xunit;

namespace CeylonQuest.E2E.Tests.Admin.Pages;

internal sealed class AdminInventoryPage(AdminUi ui, AdminDashboardPage dashboard)
{
    public const string Path = "/api/catalog/reports/inventory-summary";
    private const string Scope = ".cq-inv-report";
    public JsonElement Report { get; private set; }

    public void Open()
    {
        var baseline = ui.Sequence;
        dashboard.Open("reports", "Island-wide Listings & Inventory Report");
        AssertReport(ui.Wait(Path, baseline).Body);
    }

    private IWebElement Box(string heading) => Assert.Single(
        ui.Browser.Driver.FindElements(By.CssSelector(Scope + " .cq-report-box")),
        box => box.FindElements(By.TagName("h3")).Any(label => AdminUi.SameText(heading, label.Text, ignoreCase: true)));

    private string[] Cells(IWebElement row) => row.FindElements(By.TagName("td")).Select(cell => cell.Text).ToArray();
    private string Numeric(decimal value) => ui.Browser.Script.ExecuteScript("return String(Number(arguments[0]))", (double)value) as string
        ?? throw new InvalidOperationException("Could not format report value.");

    public void AssertReport(JsonElement report)
    {
        Report = report.Clone();
        var summary = report.GetProperty("summary");
        foreach (var key in new[] { "totalListings", "activeListings", "totalCapacity", "bookedCapacity", "remainingCapacity", "lowAvailabilityCount", "soldOutCount" })
            Assert.True(summary.Number(key) >= 0, "Negative catalog inventory statistic: " + key);
        Assert.InRange(summary.Int("activeListings"), 0, summary.Int("totalListings"));
        Assert.InRange(summary.Number("occupancyRate"), 0, 100);
        Assert.Equal(summary.Int("totalCapacity"), summary.Int("bookedCapacity") + summary.Int("remainingCapacity"));
        foreach (var (label, value, sub) in new[] {
            ("Active Listings", Numeric(summary.Number("activeListings")), $"out of {summary.Int("totalListings")} total"),
            ("Window Capacity", $"{summary.Int("totalCapacity")} spots", $"{summary.Int("remainingCapacity")} available"),
            ("Booked Spots", Numeric(summary.Number("bookedCapacity")), "Confirmed reservations"),
            ("Occupancy Rate", Numeric(summary.Number("occupancyRate")) + "%", "")
        })
        {
            ui.Browser.Until(_ => {
                var cards = ui.Browser.Driver.FindElements(By.CssSelector(Scope + " .cq-kpicard"));
                var card = cards.SingleOrDefault(element => element.FindElements(By.CssSelector(".cq-kpicard__label"))
                    .Any(heading => AdminUi.SameText(label, heading.Text, ignoreCase: true)));
                return card is not null && AdminUi.SameText(value, card.FindElement(By.CssSelector(".cq-kpicard__val")).Text) &&
                    (sub.Length == 0 || AdminUi.SameText(sub, card.FindElement(By.CssSelector(".cq-kpicard__sub")).Text));
            }, "Inventory KPI did not match the actual catalog response: " + label);
        }
        var categories = report.Array("byCategory");
        var categoryBox = Box("Category Distribution & Inventory");
        ui.Browser.Until(_ => categoryBox.FindElements(By.CssSelector("tbody tr")).Count == categories.Length,
            "Inventory category row count did not update.");
        if (categories.Length == 0) AdminUi.EqualText("No category data matching query.", categoryBox.FindElement(By.CssSelector(".cq-report-empty")).Text);
        var categoryRows = categoryBox.FindElements(By.CssSelector("tbody tr"));
        for (var index = 0; index < categories.Length; index++)
        {
            var category = categories[index];
            var occupancy = category.Int("totalCapacity") > 0
                ? Math.Round((decimal)category.Int("bookedCapacity") / category.Int("totalCapacity") * 100, MidpointRounding.AwayFromZero) : 0;
            var expected = new[] {
                category.Text("displayName", category.Text("category")), category.Int("listingCount").ToString(CultureInfo.InvariantCulture),
                $"{category.Int("totalCapacity")} spots", category.Int("bookedCapacity").ToString(CultureInfo.InvariantCulture),
                category.Int("remainingCapacity").ToString(CultureInfo.InvariantCulture), occupancy.ToString(CultureInfo.InvariantCulture) + "%"
            };
            var actual = Cells(categoryRows[index]);
            Assert.Equal(expected.Length, actual.Length);
            for (var cell = 0; cell < expected.Length; cell++) AdminUi.EqualText(expected[cell], actual[cell]);
        }
        var locations = report.Array("byLocation");
        var locationBox = Box("Regional Supply & Coverage Matrix");
        ui.Browser.Until(_ => locationBox.FindElements(By.CssSelector("tbody tr")).Count == locations.Length,
            "Inventory location row count did not update.");
        if (locations.Length == 0) AdminUi.EqualText("No locations matching current filters.", locationBox.FindElement(By.CssSelector(".cq-report-empty")).Text);
        var locationRows = locationBox.FindElements(By.CssSelector("tbody tr"));
        for (var index = 0; index < locations.Length; index++)
        {
            var location = locations[index];
            var actual = Cells(locationRows[index]);
            Assert.Equal(5, actual.Length);
            AdminUi.EqualText(location.Text("location"), actual[0]);
            AdminUi.EqualText(location.Int("listingCount").ToString(CultureInfo.InvariantCulture), actual[1]);
            AdminUi.EqualText($"{location.Int("totalCapacity")} spots", actual[2]);
            var present = location.Array("presentCategories").Select(value => CategoryName(value.GetString()!)).ToArray();
            var missing = location.Array("missingCategories").Select(value => "Missing " + CategoryName(value.GetString()!)).ToArray();
            AdminUi.EqualText(string.Join(" ", present), actual[3]);
            AdminUi.EqualText(missing.Length == 0 ? "Full Coverage" : string.Join(" ", missing), actual[4]);
        }
        ui.NoErrors(Scope);
        ui.Browser.Step("Inventory KPIs, category distribution, regional counts and coverage labels match the catalog response.");
    }

    private static string CategoryName(string value) => value switch {
        "Experience" => "Experiences", "Restaurant" => "Dining", "Accommodation" => "Stays", _ => value
    };

    public void FiltersAndReset(string kind)
    {
        var start = TestSettings.TodayInSriLanka.AddDays(1);
        var end = start.AddDays(7);
        string category = "", location = "";
        if (kind == "dates")
        {
            ui.Button("Custom Range", "//div[contains(@class,'cq-inv-report')]");
            ui.Browser.SetDate(By.Id("inv-start-date"), start);
            ui.Browser.SetDate(By.Id("inv-end-date"), end);
        }
        else if (kind == "category")
        {
            category = Report.Array("byCategory").OrderByDescending(item => item.Int("listingCount"))
                .Select(item => item.Text("category")).FirstOrDefault() ?? "Experience";
            ui.Select("inv-category", category);
        }
        else if (kind == "location")
        {
            location = TestSettings.Optional("CQ_E2E_ADMIN_CATALOG_LOCATION") ??
                Report.Array("byLocation").Select(item => item.Text("location")).FirstOrDefault() ?? "";
            Assert.False(string.IsNullOrWhiteSpace(location), "Location-filter coverage needs an existing inventory location or CQ_E2E_ADMIN_CATALOG_LOCATION.");
            ui.Browser.Fill(By.Id("inv-location"), location);
        }
        else throw new ArgumentException("Unknown inventory filter kind.", nameof(kind));
        var baseline = ui.Sequence;
        ui.Button("Apply Filters", "//div[contains(@class,'cq-inv-report')]");
        var response = ui.Wait(Path, baseline, uri =>
            AdminUi.Query(uri, "category") == category && AdminUi.Query(uri, "location") == location.Trim() &&
            (kind != "dates" || (AdminUi.Query(uri, "startDate") == start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) &&
                AdminUi.Query(uri, "endDate") == end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))));
        AssertReport(response.Body);
        if (kind == "category") Assert.All(response.Body.Array("byCategory"), item => Assert.Equal(category, item.Text("category")));
        if (kind == "location") Assert.All(response.Body.Array("byLocation"), item =>
            Assert.Contains(location.Trim(), item.Text("location"), StringComparison.OrdinalIgnoreCase));
        baseline = ui.Sequence;
        ui.Button("Reset", "//div[contains(@class,'cq-inv-report')]");
        var reset = ui.Wait(Path, baseline, uri => AdminUi.Query(uri, "category") == "" && AdminUi.Query(uri, "location") == "");
        AssertReport(reset.Body);
        ui.InputValue(By.Id("inv-category"), "");
        ui.InputValue(By.Id("inv-location"), "");
        var resetUri = new Uri(reset.Url);
        var resetStart = DateOnly.ParseExact(AdminUi.Query(resetUri, "startDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var resetEnd = DateOnly.ParseExact(AdminUi.Query(resetUri, "endDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        Assert.Equal(30, resetEnd.DayNumber - resetStart.DayNumber);
        AdminUi.EqualText("Next 30 Days", ui.Browser.Visible(By.CssSelector(Scope + " .cq-preset-btn.active")).Text);
    }
}
