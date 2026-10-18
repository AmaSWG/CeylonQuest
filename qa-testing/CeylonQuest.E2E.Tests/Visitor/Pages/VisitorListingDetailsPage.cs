using CeylonQuest.E2E.Tests.Visitor.Infrastructure;
using OpenQA.Selenium;
using Xunit;

namespace CeylonQuest.E2E.Tests.Visitor.Pages;

internal sealed class VisitorListingDetailsPage(VisitorCatalogUi ui, CatalogListing listing)
{
    private const string Scope = ".vd-detail-modal";
    private string Text(string selector) => ui.Browser.Visible(By.CssSelector(Scope + " " + selector)).Text;

    public void AssertDetails()
    {
        VisitorCatalogUi.EqualText(listing.Title, Text(".vd-detail-modal__title"));
        VisitorCatalogUi.EqualText(listing.Type, Text(".vd-type-badge"), ignoreCase: true);
        VisitorCatalogUi.EqualText("By " + listing.Provider, Text(".vd-detail-modal__provider"));
        VisitorCatalogUi.EqualText(listing.Text("description"), Text(".vd-detail-modal__desc"));
        VisitorCatalogUi.EqualText(ui.Money(listing.Price), Text(".vd-detail-price__val"));
        VisitorCatalogUi.EqualText("/ " + listing.Unit, Text(".vd-detail-price__unit"));
        Row("Location", Value("location", "Sri Lanka"));
        switch (listing.Type)
        {
            case "Experience":
                Row("Duration", Value("duration", "Flexible"));
                Row("Group Size", $"Up to {Positive("maxParticipants", 1)} people");
                Row("Schedule", Value("scheduleInfo", Value("availableDays", "Daily")));
                Row("Valid From", DisplayDate(listing.Text("validFrom")));
                Row("Valid Until", DisplayDate(listing.Text("validUntil")));
                var days = listing.Data.TryGetProperty("availableDays", out var dayValue) &&
                    dayValue.ValueKind == System.Text.Json.JsonValueKind.Array
                    ? string.Join(", ", dayValue.EnumerateArray().Select(day => day.GetString()))
                    : Value("availableDays", "Not specified");
                Row("Available Days", days);
                break;
            case "Restaurant":
                Row("Cuisine", Value("cuisineType", Value("category", "Specialty Cuisine")));
                Row("Dining Style", Value("diningStyle", "Casual Dining"));
                Row("Seating Capacity", listing.Int("seatingCapacity") > 0 ? $"{listing.Int("seatingCapacity")} seats" : "Table Seating");
                Row("Hours", Value("openingHours", "11:00 AM - 10:00 PM"));
                break;
            case "Accommodation":
                Row("Property Type", Value("propertyType", Value("category", "Stay")));
                Row("Guest Capacity", $"Up to {Positive("maxGuests", 2)} guests");
                Row("Bed Details", Value("bedDetails", "King / Queen Bed"));
                Row("Amenities", Value("amenities", "WiFi, AC, En-suite Bathroom"));
                break;
            default: throw new InvalidOperationException("Unexpected listing category.");
        }
        Assert.True(ui.Browser.Visible(By.CssSelector(Scope + " .vd-btn-book")).Enabled);
        ui.Browser.Step("Details verified against the catalog data: " + listing.Type + " / " + listing.Title);
    }

    private string Value(string key, string fallback) => listing.Text(key) is { Length: > 0 } value ? value : fallback;
    private int Positive(string key, int fallback) => listing.Int(key) > 0 ? listing.Int(key) : fallback;
    private string DisplayDate(string value) => string.IsNullOrEmpty(value) ? "Not specified" :
        ui.Browser.Script.ExecuteScript("""
            const date = new Date(String(arguments[0]).slice(0,10) + 'T00:00:00Z');
            return date.toLocaleDateString('en-GB', {day:'numeric',month:'short',year:'numeric',timeZone:'UTC'});
            """, value) as string ?? throw new InvalidOperationException("Could not format validity date.");

    private void Row(string label, string expected)
    {
        var row = ui.Browser.Driver.FindElements(By.CssSelector(Scope + " .vd-detail-modal__list .vd-detail-row"))
            .Single(element => VisitorCatalogUi.Normalize(element.FindElement(By.CssSelector(".vd-detail-row__label")).Text) == label + ":");
        VisitorCatalogUi.EqualText(expected, row.FindElement(By.CssSelector(".vd-detail-row__val")).Text);
    }

    public void Close()
    {
        ui.Browser.Click(By.CssSelector(Scope + " .vd-detail-modal__close"));
        ui.Browser.Until(driver => !driver.FindElements(By.CssSelector(Scope)).Any(element => element.Displayed),
            "Listing details did not close.");
    }

    public VisitorAvailabilityPage OpenAvailability()
    {
        ui.Browser.Click(By.CssSelector(Scope + " .vd-btn-book"));
        ui.Browser.Visible(By.CssSelector(".vd-booking-modal"));
        Assert.DoesNotContain(ui.Browser.Driver.FindElements(By.CssSelector(Scope)), element => element.Displayed);
        return new VisitorAvailabilityPage(ui, listing);
    }
}
