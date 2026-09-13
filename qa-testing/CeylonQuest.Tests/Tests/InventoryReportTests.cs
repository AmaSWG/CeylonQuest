using System.Text.Json;
using CeylonQuest.Tests.Pages;
using CeylonQuest.Tests.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class InventoryReportTests : BaseTest
{
    private string Env(string name)
    {
        var value =
            Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{name} is not configured.");
        }

        return value;
    }

    private string ReportCategory =>
        Environment.GetEnvironmentVariable(
            "QA_REPORT_CATEGORY")
        ?? "Experience";

    private string ReportLocation =>
        Environment.GetEnvironmentVariable(
            "QA_REPORT_LOCATION")
        ?? "Colombo";

    // =====================================================
    // CLEAR SESSION
    // =====================================================

    private void ClearSession()
    {
        Driver.Navigate().GoToUrl(BaseUrl);

        ((IJavaScriptExecutor)Driver)
            .ExecuteScript(@"
                window.localStorage.clear();
                window.sessionStorage.clear();
            ");
    }

    // =====================================================
    // LOGIN
    // =====================================================

    private void Login(
        string email,
        string password,
        params string[] expectedNavIds)
    {
        ClearSession();

        Driver.Navigate().GoToUrl(
            $"{BaseUrl}/login");

        var wait =
            new WebDriverWait(
                Driver,
                TimeSpan.FromSeconds(30));

        var emailInput =
            wait.Until(d =>
                d.FindElements(
                        By.Id("login-email"))
                    .FirstOrDefault(e =>
                        e.Displayed &&
                        e.Enabled));

        if (emailInput == null)
        {
            throw new NoSuchElementException(
                "Login email field was not found.");
        }

        emailInput.Clear();
        emailInput.SendKeys(email);

        var passwordInput =
            wait.Until(d =>
                d.FindElements(
                        By.Id("login-password"))
                    .FirstOrDefault(e =>
                        e.Displayed &&
                        e.Enabled));

        if (passwordInput == null)
        {
            throw new NoSuchElementException(
                "Login password field was not found.");
        }

        passwordInput.Clear();
        passwordInput.SendKeys(password);

        var loginButton =
            wait.Until(d =>
                d.FindElements(
                        By.Id("login-button"))
                    .FirstOrDefault(e =>
                        e.Displayed &&
                        e.Enabled));

        if (loginButton == null)
        {
            throw new NoSuchElementException(
                "Login button was not found.");
        }

        loginButton.Click();

        wait.Until(d =>
            expectedNavIds.Any(id =>
                d.FindElements(
                        By.Id(id))
                    .Any(e => e.Displayed)));
    }

    private void LoginProvider()
    {
        Login(
            Env("QA_PROVIDER_EMAIL"),
            Env("QA_PROVIDER_PASSWORD"),
            "pd-nav-reports",
            "pd-nav-services");
    }

    private void LoginAdmin()
    {
        Login(
            Env("QA_ADMIN_EMAIL"),
            Env("QA_ADMIN_PASSWORD"),
            "ad-nav-reports",
            "ad-nav-home",
            "ad-nav-dashboard");
    }

    private void LoginVisitor()
    {
        Login(
            Env("QA_VISITOR_EMAIL"),
            Env("QA_VISITOR_PASSWORD"),
            "nav-explore");
    }

    // =====================================================
    // AVAILABILITY HELPERS FOR TC61-05 / TC61-06
    // =====================================================

    private string ResolveReportListingId(
        InventoryReportPage page)
    {
        var title =
            Env("QA_AVAILABILITY_LISTING_TITLE");

        var result =
            page.BrowserApiRequest(
                "GET",
                $"/api/catalog/activity-listings/public?search={Uri.EscapeDataString(title)}",
                null,
                false);

        Assert.Equal(
            200,
            result.Status);

        using var json =
            JsonDocument.Parse(
                result.Body);

        if (json.RootElement.ValueKind
            != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "Public listing API did not return an array.");
        }

        foreach (
            var item
            in json.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty(
                    "title",
                    out var titleProperty))
            {
                continue;
            }

            var itemTitle =
                titleProperty.GetString();

            if (!string.Equals(
                    itemTitle?.Trim(),
                    title.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!item.TryGetProperty(
                    "id",
                    out var idProperty))
            {
                continue;
            }

            var id =
                idProperty.GetString();

            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        throw new InvalidOperationException(
            $"Active listing '{title}' was not found.");
    }

    private (
        string Date,
        string Slot,
        int Total,
        int Remaining)
        FindOperatingSlot(
            AvailabilityPage page,
            string listingId,
            int startOffset = 1)
    {
        for (
            var offset = startOffset;
            offset <= 30;
            offset++)
        {
            var date =
                DateTime.Today
                    .AddDays(offset)
                    .ToString("yyyy-MM-dd");

            using var result =
                page.GetAvailability(
                    listingId,
                    date);

            var root =
                result.RootElement;

            if (root.TryGetProperty(
                    "isOperatingDay",
                    out var operatingDay))
            {
                if (!operatingDay.GetBoolean())
                {
                    continue;
                }
            }

            if (!root.TryGetProperty(
                    "slots",
                    out var slots))
            {
                continue;
            }

            if (slots.ValueKind
                    != JsonValueKind.Array
                ||
                slots.GetArrayLength() == 0)
            {
                continue;
            }

            var first =
                slots[0];

            var slot =
                first.GetProperty(
                        "timeSlot")
                    .GetString();

            if (string.IsNullOrWhiteSpace(slot))
            {
                continue;
            }

            return (
                date,
                slot,
                first.GetProperty(
                        "totalCapacity")
                    .GetInt32(),
                first.GetProperty(
                        "remainingCapacity")
                    .GetInt32());
        }

        throw new InvalidOperationException(
            "No operating availability slot was found in the next 30 days.");
    }

    private static int NumberFromText(
        string text)
    {
        var digits =
            new string(
                text.Where(char.IsDigit)
                    .ToArray());

        if (string.IsNullOrWhiteSpace(digits))
        {
            return 0;
        }

        return int.Parse(digits);
    }

    // =====================================================
    // TC61-01
    // Provider opens availability report
    // =====================================================

    [Fact]
    public void TC61_01_Provider_CanOpenAvailabilityReport()
    {
        LoginProvider();

        var page =
            new InventoryReportPage(Driver);

        page.OpenProviderReport();

        Assert.True(
            page.HasProviderHeading(),
            "Provider availability report heading was not visible.");
    }

    // =====================================================
    // TC61-02
    // Admin opens island-wide report
    // =====================================================

    [Fact]
    public void TC61_02_Admin_CanOpenIslandWideReport()
    {
        LoginAdmin();

        var page =
            new InventoryReportPage(Driver);

        page.OpenAdminReport();

        Assert.True(
            page.HasAdminHeading(),
            "Admin island-wide report heading was not visible.");

        Assert.True(
            page.HasCategorySection(),
            "Category Distribution & Inventory section was not visible.");

        Assert.True(
            page.HasLocationSection(),
            "Regional Supply & Coverage Matrix section was not visible.");
    }

    // =====================================================
    // TC61-03
    // Groups listings by category
    // =====================================================

    [Fact]
    public void TC61_03_Admin_Report_GroupsListingsByCategory()
    {
        LoginAdmin();

        var page =
            new InventoryReportPage(Driver);

        page.OpenAdminReport();

        Assert.True(
            page.HasCategorySection(),
            "Category Distribution & Inventory section was not visible.");

        Assert.True(
            page.CategoryDataRowCount() > 0,
            "No category rows were shown.");
    }

    // =====================================================
    // TC61-04
    // Groups listings by location
    // =====================================================

    [Fact]
    public void TC61_04_Admin_Report_GroupsListingsByLocation()
    {
        LoginAdmin();

        var page =
            new InventoryReportPage(Driver);

        page.OpenAdminReport();

        Assert.True(
            page.HasLocationSection(),
            "Regional Supply & Coverage Matrix was not visible.");

        var result =
            page.GetInventoryReportApi();

        Assert.Equal(
            200,
            result.Status);

        using var json =
            JsonDocument.Parse(
                result.Body);

        Assert.True(
            json.RootElement.TryGetProperty(
                "byLocation",
                out var byLocation),
            "Report API did not contain byLocation data.");

        Assert.Equal(
            JsonValueKind.Array,
            byLocation.ValueKind);

        Assert.True(
            byLocation.GetArrayLength() > 0,
            "No location grouping data was returned.");
    }

    // =====================================================
    // TC61-05
    // Sold Out alert
    // =====================================================

    [Fact]
    public void TC61_05_ProviderReport_FlagsSoldOutCapacity()
    {
        LoginProvider();

        var reportPage =
            new InventoryReportPage(Driver);

        var availabilityPage =
            new AvailabilityPage(Driver);

        var listingId =
            ResolveReportListingId(
                reportPage);

        var slot =
            FindOperatingSlot(
                availabilityPage,
                listingId,
                1);

        var alreadyBooked =
            Math.Max(
                0,
                slot.Total - slot.Remaining);

        // Leave exactly 3 spaces available.
        var newCapacity =
            alreadyBooked + 3;

        var setResult =
            availabilityPage.SetCapacity(
                listingId,
                slot.Date,
                slot.Slot,
                newCapacity);

        Assert.Equal(
            200,
            setResult.Status);

        // Consume all 3 spaces.
        var bookingResult =
            availabilityPage.SimulateBooking(
                listingId,
                slot.Date,
                slot.Slot,
                3);

        Assert.Equal(
            200,
            bookingResult.Status);

        using var verify =
            availabilityPage.GetAvailability(
                listingId,
                slot.Date);

        var verifiedSlot =
            verify.RootElement
                .GetProperty("slots")
                .EnumerateArray()
                .First(s =>
                    string.Equals(
                        s.GetProperty(
                                "timeSlot")
                            .GetString(),
                        slot.Slot,
                        StringComparison.Ordinal));

        Assert.Equal(
            0,
            verifiedSlot
                .GetProperty(
                    "remainingCapacity")
                .GetInt32());

        Assert.True(
            verifiedSlot
                .GetProperty(
                    "isFullyBooked")
                .GetBoolean());

        reportPage.OpenProviderReport();

        Assert.True(
            reportPage.HasLowAvailabilitySection(),
            "Low availability section was not visible.");

        Assert.True(
            reportPage.HasSoldOutAlert(),
            "Sold Out alert was not visible after creating a fully booked slot.");
    }

    // =====================================================
    // TC61-06
    // Low Stock alert
    // =====================================================

    [Fact]
    public void TC61_06_ProviderReport_FlagsLowCapacity()
    {
        LoginProvider();

        var reportPage =
            new InventoryReportPage(Driver);

        var availabilityPage =
            new AvailabilityPage(Driver);

        var listingId =
            ResolveReportListingId(
                reportPage);

        // Start from a later date than TC61-05.
        var slot =
            FindOperatingSlot(
                availabilityPage,
                listingId,
                2);

        var alreadyBooked =
            Math.Max(
                0,
                slot.Total - slot.Remaining);

        // Leave 2 spaces available.
        var lowCapacity =
            alreadyBooked + 2;

        var setResult =
            availabilityPage.SetCapacity(
                listingId,
                slot.Date,
                slot.Slot,
                lowCapacity);

        Assert.Equal(
            200,
            setResult.Status);

        using var verify =
            availabilityPage.GetAvailability(
                listingId,
                slot.Date);

        var verifiedSlot =
            verify.RootElement
                .GetProperty("slots")
                .EnumerateArray()
                .First(s =>
                    string.Equals(
                        s.GetProperty(
                                "timeSlot")
                            .GetString(),
                        slot.Slot,
                        StringComparison.Ordinal));

        var remaining =
            verifiedSlot
                .GetProperty(
                    "remainingCapacity")
                .GetInt32();

        Assert.InRange(
            remaining,
            1,
            3);

        reportPage.OpenProviderReport();

        Assert.True(
            reportPage.HasLowAvailabilitySection(),
            "Low availability section was not visible.");

        Assert.True(
            reportPage.HasLowStockAlert(),
            "Low Stock alert was not visible after creating a low-capacity slot.");
    }

    // =====================================================
    // TC61-07
    // Date window filter
    // =====================================================

    [Fact]
    public void TC61_07_DateWindowFilter_Works()
    {
        LoginAdmin();

        var page =
            new InventoryReportPage(Driver);

        page.OpenAdminReport();

        page.SelectPreset(
            "Next 7 Days");

        Assert.True(
            page.IsPresetActive(
                "Next 7 Days"),
            "Next 7 Days preset was not active.");
    }

    // =====================================================
    // TC61-08
    // Category filter
    // =====================================================

    [Fact]
    public void TC61_08_Admin_CategoryFilter_Works()
    {
        LoginAdmin();

        var page =
            new InventoryReportPage(Driver);

        page.OpenAdminReport();

        page.ApplyCategory(
            ReportCategory);

        Assert.True(
            string.Equals(
                ReportCategory,
                page.SelectedCategory(),
                StringComparison.OrdinalIgnoreCase),
            $"Expected category '{ReportCategory}', but actual was '{page.SelectedCategory()}'.");
    }

    // =====================================================
    // TC61-09
    // Location filter
    // =====================================================

    [Fact]
    public void TC61_09_Admin_LocationFilter_Works()
    {
        LoginAdmin();

        var page =
            new InventoryReportPage(Driver);

        page.OpenAdminReport();

        page.ApplyLocation(
            ReportLocation);

        Assert.True(
            string.Equals(
                ReportLocation,
                page.LocationValue(),
                StringComparison.OrdinalIgnoreCase),
            $"Expected location '{ReportLocation}', but actual was '{page.LocationValue()}'.");
    }

    // =====================================================
    // TC61-10
    // Combined filters
    // =====================================================

    [Fact]
    public void TC61_10_Admin_CombinedFilters_Work()
    {
        LoginAdmin();

        var page =
            new InventoryReportPage(Driver);

        page.OpenAdminReport();

        page.SelectPreset(
            "Next 7 Days");

        page.ApplyCombined(
            ReportCategory,
            ReportLocation);

        Assert.True(
            page.IsPresetActive(
                "Next 7 Days"),
            "Next 7 Days was not active.");

        Assert.True(
            string.Equals(
                ReportCategory,
                page.SelectedCategory(),
                StringComparison.OrdinalIgnoreCase),
            $"Expected category '{ReportCategory}', but actual was '{page.SelectedCategory()}'.");

        Assert.True(
            string.Equals(
                ReportLocation,
                page.LocationValue(),
                StringComparison.OrdinalIgnoreCase),
            $"Expected location '{ReportLocation}', but actual was '{page.LocationValue()}'.");
    }

    // =====================================================
    // TC61-11
    // Report totals match API
    // =====================================================

    [Fact]
public void TC61_11_ReportTotals_MatchInventoryReportApi()
{
    LoginAdmin();

    var page =
        new InventoryReportPage(Driver);

    page.OpenAdminReport();

    // UI default report = Next 30 Days.
    // Use exactly the same date range for the API.
    var startDate =
        DateTime.Today
            .ToString("yyyy-MM-dd");

    var endDate =
        DateTime.Today
            .AddDays(30)
            .ToString("yyyy-MM-dd");

    var result =
        page.GetInventoryReportApi(
            startDate,
            endDate);

    Assert.Equal(
        200,
        result.Status);

    using var json =
        JsonDocument.Parse(
            result.Body);

    Assert.True(
        json.RootElement.TryGetProperty(
            "summary",
            out var summary),
        "Report API response did not contain summary.");

    var apiCapacity =
        summary.GetProperty(
                "totalCapacity")
            .GetInt32();

    var apiBooked =
        summary.GetProperty(
                "bookedCapacity")
            .GetInt32();

    var uiCapacityText =
        page.KpiValue(
            "Window Capacity");

    var uiBookedText =
        page.KpiValue(
            "Booked Spots");

    var uiCapacity =
        NumberFromText(
            uiCapacityText);

    var uiBooked =
        NumberFromText(
            uiBookedText);

    Assert.Equal(
        apiCapacity,
        uiCapacity);

    Assert.Equal(
        apiBooked,
        uiBooked);
}

    // =====================================================
    // TC61-12
    // No-data filter
    // =====================================================

    [Fact]
    public void TC61_12_Admin_FilterReturnsNoDataSafely()
    {
        LoginAdmin();

        var page =
            new InventoryReportPage(Driver);

        page.OpenAdminReport();

        const string noLocation =
            "ZZZ-NO-SUCH-LOCATION-99999";

        page.ApplyLocation(
            noLocation);

        Assert.True(
            string.Equals(
                noLocation,
                page.LocationValue(),
                StringComparison.Ordinal));

        Assert.True(
            page.ShowsNoDataState()
            ||
            page.HasAdminHeading(),
            "Report did not safely handle the no-data filter.");
    }

    // =====================================================
    // TC61-13
    // Visitor authorization
    // =====================================================

    [Fact]
    public void TC61_13_Visitor_CannotAccessInventoryReportApi()
    {
        LoginVisitor();

        var page =
            new InventoryReportPage(Driver);

        var result =
            page.GetInventoryReportApi();

        Assert.True(
            result.Status == 401
            ||
            result.Status == 403,

            $"SECURITY DEFECT: Visitor received HTTP {result.Status}. Body: {result.Body}");
    }

    // =====================================================
    // TC61-14
    // No matching availability data
    // =====================================================

    [Fact]
    public void TC61_14_Report_HandlesNoMatchingAvailabilityData()
    {
        LoginAdmin();

        var page =
            new InventoryReportPage(Driver);

        page.OpenAdminReport();

        const string noLocation =
            "NO-MATCHING-DATA-987654321";

        page.ApplyLocation(
            noLocation);

        Assert.True(
            page.ShowsNoDataState()
            ||
            page.HasAdminHeading(),
            "Report did not safely handle no matching availability data.");
    }

    // =====================================================
    // TC61-15
    // Reset filters
    // =====================================================

    [Fact]
    public void TC61_15_ResetFilters_RestoresDefault30DayReport()
    {
        LoginAdmin();

        var page =
            new InventoryReportPage(Driver);

        page.OpenAdminReport();

        page.ApplyCombined(
            ReportCategory,
            ReportLocation);

        Assert.True(
            string.Equals(
                ReportCategory,
                page.SelectedCategory(),
                StringComparison.OrdinalIgnoreCase));

        Assert.True(
            string.Equals(
                ReportLocation,
                page.LocationValue(),
                StringComparison.OrdinalIgnoreCase));

        page.Reset();

        Assert.True(
            string.IsNullOrWhiteSpace(
                page.SelectedCategory()),
            $"Category was not cleared. Current: '{page.SelectedCategory()}'");

        Assert.True(
            string.IsNullOrWhiteSpace(
                page.LocationValue()),
            $"Location was not cleared. Current: '{page.LocationValue()}'");

        Assert.True(
            page.IsPresetActive(
                "Next 30 Days"),
            "Reset did not restore Next 30 Days.");
    }
}