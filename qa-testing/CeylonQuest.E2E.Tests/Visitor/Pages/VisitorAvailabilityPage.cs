using System.Globalization;
using System.Text.Json;
using CeylonQuest.E2E.Tests.Infrastructure;
using CeylonQuest.E2E.Tests.Visitor.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.E2E.Tests.Visitor.Pages;

internal sealed class VisitorAvailabilityPage(VisitorCatalogUi ui, CatalogListing listing)
{
    private const string Scope = ".vd-booking-modal";
    private static readonly By DateInput = By.CssSelector(Scope + " .vd-booking-form-grid input[type='date']");
    private static readonly By Checkout = By.XPath("//div[contains(@class,'vd-booking-form-grid')]//label[normalize-space()='Check-out Date']/following-sibling::input");
    private static readonly By SlotSelect = By.CssSelector(Scope + " .vd-booking-form-grid select");
    private static readonly By GuestsInput = By.CssSelector(Scope + " input[type='number']");
    private static readonly By Submit = By.CssSelector(Scope + " .vd-btn-book");

    public void PrepareAndAssertValidSelection()
    {
        VisitorCatalogSettings.ValidateDate(listing.Type);
        VisitorCatalogUi.EqualText(listing.Type == "Restaurant" ? "Reserve a Table" : "Book: " + listing.Title,
            ui.Browser.Visible(By.CssSelector(Scope + " .vd-detail-modal__title")).Text);
        var providerLines = ui.Browser.Driver.FindElements(By.CssSelector(Scope + " .vd-detail-modal__provider")).Select(element => element.Text).ToArray();
        Assert.Contains("By " + listing.Provider, providerLines);
        if (listing.Type == "Restaurant") Assert.Contains(listing.Title, providerLines);
        var date = VisitorCatalogSettings.Date(listing.Type);
        var dateText = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (listing.Type == "Experience")
        {
            foreach (var (key, lowerBound) in new[] { ("validFrom", true), ("validUntil", false) })
            {
                var value = listing.Text(key);
                if (string.IsNullOrEmpty(value)) continue;
                var bound = DateOnly.ParseExact(value[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                Assert.True(lowerBound ? date >= bound : date <= bound, "Configured experience date is outside its validity period.");
            }
        }
        ui.Browser.SetDate(DateInput, date);
        DateOnly? checkout = null;
        if (listing.Type == "Accommodation")
        {
            checkout = TestSettings.Date("CQ_E2E_CATALOG_CHECK_OUT");
            Assert.True(checkout.Value.DayNumber - date.DayNumber >= Math.Max(1, listing.Int("minStayNights")),
                "Configured accommodation dates do not meet the listing's minimum stay.");
            ui.Browser.SetDate(Checkout, checkout.Value);
        }
        var response = ui.WaitResponse("/api/catalog/availability/" + listing.Id, date: dateText);
        var data = response.Body;
        Assert.Equal(listing.Id, data.GetProperty("listingId").GetString(), ignoreCase: true);
        Assert.Equal(dateText, data.GetProperty("date").GetString());
        Assert.True(data.GetProperty("isOperatingDay").GetBoolean(), "Configured date must be an operating day.");
        Assert.False(data.GetProperty("isFullyBooked").GetBoolean(), "Configured date must have available capacity.");
        var slots = data.GetProperty("slots").EnumerateArray().Select(slot => slot.Clone()).ToArray();
        Assert.NotEmpty(slots);
        var guests = VisitorCatalogSettings.Guests;
        var maxGuests = listing.Type switch {
            "Accommodation" => listing.Int("maxGuests"),
            "Restaurant" => listing.Int("seatingCapacity"),
            _ => listing.Int("maxParticipants")
        };
        Assert.InRange(guests, 1, maxGuests);
        var usable = slots.Where(slot => !slot.GetProperty("isFullyBooked").GetBoolean() &&
            slot.GetProperty("remainingCapacity").GetInt32() >= (listing.Type == "Accommodation" ? 1 : guests)).ToArray();
        Assert.True(usable.Length > 0, "Test date requires a slot with enough room/seat/participant capacity.");
        var selected = usable[0];
        var slotName = selected.GetProperty("timeSlot").GetString()!;
        ui.Browser.Until(_ => {
            var select = new SelectElement(ui.Browser.Visible(SlotSelect));
            return select.WrappedElement.Enabled && select.Options.Count == slots.Length + 1 &&
                select.Options.Skip(1).Select(option => option.GetDomAttribute("value"))
                    .SequenceEqual(slots.Select(slot => slot.GetProperty("timeSlot").GetString()));
        }, "Availability options did not match the selected listing/date response.");
        var options = new SelectElement(ui.Browser.Visible(SlotSelect)).Options.Skip(1).ToArray();
        for (var index = 0; index < slots.Length; index++) AssertSlot(options[index], slots[index]);
        Assert.True(options.Single(option => option.GetDomAttribute("value") == slotName).Enabled,
            "Configured future slot is not selectable; check the restaurant reservation lead time.");
        new SelectElement(ui.Browser.Visible(SlotSelect)).SelectByValue(slotName);
        ui.Browser.Fill(GuestsInput, guests.ToString(CultureInfo.InvariantCulture));
        ui.Browser.Visible(GuestsInput).SendKeys(Keys.Tab);
        ui.Browser.Until(driver => driver.FindElement(Submit).Enabled, "Valid availability selection did not enable the booking button.");
        Assert.Equal(dateText, ui.Browser.Visible(DateInput).GetDomProperty("value"));
        Assert.Equal(slotName, new SelectElement(ui.Browser.Visible(SlotSelect)).SelectedOption.GetDomAttribute("value"));
        Assert.Equal(guests.ToString(CultureInfo.InvariantCulture), ui.Browser.Visible(GuestsInput).GetDomProperty("value"));
        if (checkout.HasValue) Assert.Equal(checkout.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ui.Browser.Visible(Checkout).GetDomProperty("value"));
        var status = ui.Browser.Visible(By.CssSelector(Scope + " .vd-avail-success")).Text;
        Assert.Contains(dateText, status);
        var remaining = selected.GetProperty("remainingCapacity").GetInt32();
        Assert.Contains(listing.Type == "Accommodation" ? "room available" : listing.Type == "Restaurant"
            ? $"{remaining} seat(s)" : $"{remaining} place(s)", status);
        Assert.DoesNotContain(ui.Browser.Driver.FindElements(By.CssSelector(Scope + " .vd-avail-error")), element => element.Displayed);
        var total = listing.Price * (listing.Type == "Accommodation" ? checkout!.Value.DayNumber - date.DayNumber : guests);
        VisitorCatalogUi.EqualText(ui.Money(total), ui.Browser.Visible(By.CssSelector(Scope + " .vd-detail-price__val")).Text);
        ui.Browser.Capture("valid-availability-" + listing.Type.ToLowerInvariant());
        ui.Browser.Step($"Verified {listing.Type} availability on {dateText}; booking submission was not clicked.");
    }

    private void AssertSlot(IWebElement option, JsonElement slot)
    {
        var remaining = slot.GetProperty("remainingCapacity").GetInt32();
        var total = slot.GetProperty("totalCapacity").GetInt32();
        Assert.InRange(remaining, 0, total);
        var name = slot.GetProperty("timeSlot").GetString()!;
        Assert.Equal(name, option.GetDomAttribute("value"));
        if (remaining <= 0 || slot.GetProperty("isFullyBooked").GetBoolean()) Assert.False(option.Enabled);
        else Assert.True(option.Enabled, "Future available slot should be enabled.");
        Assert.Contains(name, option.Text);
        if (listing.Type == "Experience") Assert.Contains(remaining > 0 ? $"{remaining} places left" : "Fully Booked", option.Text);
        if (listing.Type == "Accommodation") Assert.Contains(remaining > 0 ? "Available" : "Fully Booked", option.Text);
    }

    public void Close()
    {
        ui.Browser.Click(By.CssSelector(Scope + " .vd-detail-modal__close"));
        ui.Browser.Until(driver => !driver.FindElements(By.CssSelector(Scope)).Any(element => element.Displayed),
            "Availability modal did not close.");
        ui.AssertNoBusinessWrites();
    }
}
