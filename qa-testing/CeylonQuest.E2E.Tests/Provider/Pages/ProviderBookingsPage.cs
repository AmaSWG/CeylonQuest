using System.Globalization;
using System.Text.Json;
using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using OpenQA.Selenium;
using Xunit;

namespace CeylonQuest.E2E.Tests.Provider.Pages;

internal sealed class ProviderBookingsPage(ProviderUi ui, ProviderDashboardPage dashboard)
{
    public JsonElement[] Records { get; private set; } = [];
    public string OriginUrl { get; private set; } = "";
    public void Open()
    {
        var before = ui.Sequence;
        dashboard.Navigate("bookings");
        var response = ui.Wait("/api/provider-bookings/my", before);
        OriginUrl = response.Url;
        Assert.Equal(JsonValueKind.Array, response.Body.ValueKind);
        Records = response.Body.EnumerateArray().ToArray();
        ui.Browser.Visible(By.XPath("//h1[normalize-space()='Booking Management']"));
        ui.Browser.Visible(By.CssSelector("input[aria-label='Search bookings']"));
        ui.Browser.Until(d => d.FindElements(By.CssSelector(".pd-bookings-table tbody tr")).Count == Records.Length,
            "Provider booking rows do not match the loaded response.");
        foreach (var booking in Records)
            ui.Browser.Step("Provider booking ID: " + booking.Text("id") + " / " + booking.Text("serviceName") + " / " + booking.Text("status"));
    }

    public void AssertDetails(string id)
    {
        var booking = Assert.Single(Records, record => record.Text("id") == id);
        Assert.False(string.IsNullOrWhiteSpace(booking.Text("serviceName")));
        Assert.True(booking.Int("peopleCount") > 0);
        Assert.True(booking.Money("totalAmount") >= 0);
        var row = Search(id);
        ProviderUi.EqualText("#" + id.Split('-')[0].ToUpperInvariant(), row.FindElement(By.CssSelector(".pd-booking-id")).Text);
        Assert.Contains(booking.Text("serviceName"), row.Text);
        ProviderUi.EqualText(DisplayStatus(booking.Text("status")), row.FindElement(By.CssSelector(".cq-status-badge")).Text, true);
        row.FindElement(By.XPath(".//button[normalize-space()='View Details']")).Click();
        ProviderUi.EqualText(booking.Text("serviceName"), ui.Browser.Visible(By.Id("booking-details-title")).Text);
        ProviderUi.EqualText(booking.Text("customerName", "Visitor"), Detail("Customer Name"));
        if (!string.IsNullOrWhiteSpace(booking.Text("customerEmail")))
            ProviderUi.EqualText(booking.Text("customerEmail"), Detail("Email"));
        ProviderUi.EqualText(DisplayStatus(booking.Text("status")), ui.Browser.Visible(By.CssSelector(".pd-booking-detail-status .cq-status-badge")).Text, true);
        ProviderUi.EqualText(ProviderUi.BrowserMoney(ui.Browser, booking.Money("totalAmount"), 0), Detail("Total Amount"));
        if (booking.Text("bookingType") == "Accommodation Booking")
        {
            ProviderUi.EqualText(DateText(booking.Date("checkInDate")), Detail("Check-in"));
            ProviderUi.EqualText(DateText(booking.Date("checkOutDate")), Detail("Check-out"));
            ProviderUi.EqualText(booking.Int("peopleCount").ToString(CultureInfo.InvariantCulture), Detail("Guests"));
            ProviderUi.EqualText(booking.Int("numberOfNights").ToString(CultureInfo.InvariantCulture), Detail("Number of Nights"));
        }
        else
        {
            ProviderUi.EqualText(DateText(booking.Date("date")), Detail("Date"));
            ProviderUi.EqualText(booking.Int("peopleCount").ToString(CultureInfo.InvariantCulture),
                Detail(booking.Text("bookingType") == "Restaurant Reservation" ? "Party Size" : "Participants"));
        }
        ui.Browser.Capture("provider-booking-details");
        ui.Browser.Click(By.CssSelector(".pd-booking-modal__close"));
    }

    public void AssertStatusFilter(string id)
    {
        var booking = Assert.Single(Records, record => record.Text("id") == id);
        var expected = DisplayStatus(booking.Text("status"));
        ui.Browser.Fill(By.CssSelector("input[aria-label='Search bookings']"), "");
        ui.Browser.Click(By.XPath($"//div[@aria-label='Booking status filters']/button[starts-with(normalize-space(),{ProviderUi.Literal(expected)})]"));
        var selectedFilter = ui.Browser.Visible(By.CssSelector(".pd-bookings-filter.active"));
        Assert.StartsWith(expected, selectedFilter.Text, StringComparison.OrdinalIgnoreCase);
        var expectedCount = Records.Count(record => DisplayStatus(record.Text("status")) == expected);
        Assert.True(expectedCount > 0, "Configured booking must exercise a populated status filter.");
        ui.Browser.Until(d => d.FindElements(By.CssSelector(".pd-bookings-table tbody tr")).Count == expectedCount,
            "Status filter did not render the expected number of bookings.");
        foreach (var row in ui.Browser.Driver.FindElements(By.CssSelector(".pd-bookings-table tbody tr")))
            ProviderUi.EqualText(expected, row.FindElement(By.CssSelector(".cq-status-badge")).Text, true);
        Search(id);
    }

    public void VerifyAttendance(string id, string status)
    {
        ProviderSettings.AssertWriteTarget(OriginUrl);
        Assert.Contains(status, new[] { "Attended", "NoShow" });
        var booking = Assert.Single(Records, record => record.Text("id") == id);
        Assert.Contains(booking.Text("status"), new[] { "Confirmed", "Completed" });
        Assert.Equal("Pending", booking.Text("attendanceStatus", "Pending"));
        Assert.False(string.IsNullOrWhiteSpace(booking.Text("scheduledEndAtUtc")),
            "The dedicated attendance booking must have a verifiable scheduled end.");
        Assert.True(DateTimeOffset.Parse(booking.Text("scheduledEndAtUtc"), CultureInfo.InvariantCulture) <= DateTimeOffset.UtcNow,
            "Attendance may only be changed after the scheduled booking has ended.");
        var row = Search(id);
        var before = ui.Sequence;
        row.FindElement(By.XPath($".//button[normalize-space()={ProviderUi.Literal(status == "Attended" ? "Verify Attended" : "Mark No-Show")}]")).Click();
        ui.Browser.Until(_ => {
            var response = ui.Latest("/api/provider-bookings/my", before);
            if (response is null) return false;
            Assert.InRange(response.Status, 200, 299);
            var saved = Assert.Single(response.Body.EnumerateArray(), record => record.Text("id") == id);
            return saved.Text("attendanceStatus") == status;
        }, "Attendance was not persisted for the dedicated booking.");
        ProviderUi.EqualText(status, Search(id).FindElement(By.CssSelector(".cq-attendance-actions p strong")).Text, true);
        ui.Browser.Step("Persisted attendance " + status + " for dedicated booking " + id + "; no reverse action exists.");
    }

    private IWebElement Search(string id)
    {
        ui.Browser.Fill(By.CssSelector("input[aria-label='Search bookings']"), id);
        return ui.Browser.Until(d => {
            var rows = d.FindElements(By.CssSelector(".pd-bookings-table tbody tr"));
            return rows.Count == 1 && rows[0].FindElement(By.CssSelector(".pd-booking-id")).Text
                .Equals("#" + id.Split('-')[0], StringComparison.OrdinalIgnoreCase) ? rows[0] : null!;
        }, "Configured full booking ID did not identify exactly one visible provider booking.");
    }
    private string Detail(string label) => ui.Browser.Visible(By.XPath(
        $"//div[contains(@class,'pd-booking-detail-item')][span[normalize-space()={ProviderUi.Literal(label)}]]/strong")).Text;
    private static string DateText(DateOnly date) => date.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
    private static string DisplayStatus(string status) => status == "PendingPayment" ? "Pending" : status;
}
