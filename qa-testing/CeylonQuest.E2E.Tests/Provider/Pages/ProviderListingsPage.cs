using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CeylonQuest.E2E.Tests.Infrastructure;
using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.E2E.Tests.Provider.Pages;

internal sealed class ProviderListingsPage(ProviderUi ui, ProviderDashboardPage dashboard)
{
    private static readonly By Rows = By.CssSelector(".pd-activities-tab .pd-table tbody tr");
    private static readonly By SearchInput = By.CssSelector(".pd-activities-tab .pd-search-input");
    private static readonly By Form = By.CssSelector(".pd-modal__form");
    private string Prefix => dashboard.ListingType switch { "Accommodation" => "hotel", "Restaurant" => "rest", _ => "exp" };
    private string NameId => dashboard.ListingType switch { "Accommodation" => "hotel-room", "Restaurant" => "rest-name", _ => "exp-title" };
    private string EmptyTitle => dashboard.ListingType switch {
        "Accommodation" => "No accommodation listings found", "Restaurant" => "No dining listing found", _ => "No experience listings found"
    };

    public void Open()
    {
        dashboard.Navigate("services");
        ui.Browser.Visible(By.Id("add-listing-btn"));
        ProviderUi.EqualText(dashboard.PageTitle, ui.Browser.Visible(By.CssSelector(".pd-activities-tab h1")).Text);
        AssertRows(dashboard.Listings);
        AssertCounts();
    }

    public void Reset()
    {
        ui.Browser.Fill(SearchInput, "");
        Status("all");
        AssertRows(dashboard.Listings);
    }

    public JsonElement[] SearchKnown(JsonElement item)
    {
        var query = item.ListingName().ToUpperInvariant();
        ui.Browser.Fill(SearchInput, query);
        var expected = dashboard.Listings.Where(listing => new[] {
            listing.ListingName(), listing.Text("description"), listing.Text("location")
        }.Any(text => text.Contains(query, StringComparison.OrdinalIgnoreCase))).ToArray();
        Assert.Contains(expected, listing => listing.Text("id") == item.Text("id"));
        AssertRows(expected);
        return expected;
    }

    public void AssertSearchAndClear(JsonElement item)
    {
        SearchKnown(item);
        ui.Browser.Fill(SearchInput, "");
        AssertRows(dashboard.Listings);
    }

    public void Status(string status)
    {
        var count = status == "all" ? dashboard.Listings.Length :
            dashboard.Listings.Count(item => item.Flag("isActive") == (status == "active"));
        var label = status switch { "active" => "Active", "inactive" => "Inactive", "all" => "All",
            _ => throw new ArgumentException("Unknown status.", nameof(status)) };
        ui.ClickText("//div[contains(@class,'pd-filter-pills')]", $"{label} ({count})");
        ui.Browser.Until(_ => ProviderUi.Normalize(ui.Browser.Visible(By.CssSelector(".pd-filter-pill.active")).Text) == $"{label} ({count})",
            "Listing status filter did not become active.");
    }

    public void AssertStatus(string status)
    {
        Status(status);
        AssertRows(status == "all" ? dashboard.Listings :
            dashboard.Listings.Where(item => item.Flag("isActive") == (status == "active")).ToArray());
        AssertCounts();
    }

    private void AssertCounts()
    {
        var labels = ui.Browser.Driver.FindElements(By.CssSelector(".pd-filter-pills .pd-filter-pill")).Select(element => element.Text).ToArray();
        Assert.Equal(new[] {
            $"All ({dashboard.Listings.Length})",
            $"Active ({dashboard.Listings.Count(item => item.Flag("isActive"))})",
            $"Inactive ({dashboard.Listings.Count(item => !item.Flag("isActive"))})"
        }, labels);
    }

    public void AssertRows(IReadOnlyList<JsonElement> expected)
    {
        ui.Browser.Until(driver => {
            var rows = driver.FindElements(Rows).Where(element => element.Displayed).ToArray();
            return rows.Length == expected.Count && rows.Select((row, index) =>
                row.FindElement(By.CssSelector("td:first-child > div:first-child")).Text == expected[index].ListingName()).All(match => match);
        }, "Provider listing rows did not match the real catalog response and selected filters.");
        if (expected.Count == 0)
        {
            ProviderUi.EqualText(EmptyTitle, ui.Browser.Visible(By.CssSelector(".pd-activities-tab .pd-empty__title")).Text);
            return;
        }
        var actual = ui.Browser.Driver.FindElements(Rows).Where(element => element.Displayed).ToArray();
        for (var index = 0; index < expected.Count; index++) AssertRow(actual[index], expected[index]);
    }

    private void AssertRow(IWebElement row, JsonElement item)
    {
        var cells = row.FindElements(By.CssSelector("td"));
        Assert.Equal(7, cells.Count);
        ProviderUi.EqualText(item.ListingName(), cells[0].FindElement(By.CssSelector("div:first-child")).Text);
        var description = item.Text("description");
        ProviderUi.EqualText(description.Length > 60 ? description[..60] + "\u2026" : description,
            cells[0].FindElements(By.CssSelector("div"))[1].Text);
        if (dashboard.ListingType == "Restaurant")
        {
            ProviderUi.EqualText(item.Text("cuisineType") + " " + item.Text("diningStyle"), cells[1].Text);
            ProviderUi.EqualText(item.Text("location"), cells[2].Text);
            ProviderUi.EqualText(ui.Money(item.Money("pricePerPerson")) + " " + item.Text("priceRange"), cells[3].Text);
            var group = item.Text("groupSizeCategory");
            ProviderUi.EqualText(group == "Large Group (More than 10)" ? $"Large Group {item.Int("seatingCapacity")} seats" :
                string.IsNullOrEmpty(group) ? $"{item.Int("seatingCapacity")} seats" : group, cells[4].Text);
        }
        else if (dashboard.ListingType == "Accommodation")
        {
            ProviderUi.EqualText(item.Text("propertyType"), cells[1].Text);
            ProviderUi.EqualText(item.Text("location"), cells[2].Text);
            ProviderUi.EqualText(ui.Money(item.Money("pricePerNight")), cells[3].Text);
            ProviderUi.EqualText($"{item.Int("maxGuests")} guests", cells[4].Text);
        }
        else
        {
            ProviderUi.EqualText(item.Text("location"), cells[1].Text);
            ProviderUi.EqualText(ui.Money(item.Money("price")) + " / " + item.Text("unit"), cells[2].Text);
            var slots = item.Text("timeSlots");
            ProviderUi.EqualText(string.IsNullOrEmpty(slots) ? "\u2014" :
                string.Join(" ", slots.Split(',', StringSplitOptions.TrimEntries)), cells[3].Text);
            ProviderUi.EqualText($"{item.Int("maxParticipants")} guests", cells[4].Text);
        }
        // Selenium .Text includes the badge's CSS text-transform: uppercase.
        ProviderUi.EqualText(item.Flag("isActive") ? "Active" : "Inactive", cells[5].Text, ignoreCase: true);
        Assert.Equal("Click to toggle status", cells[5].FindElement(By.TagName("button")).GetDomAttribute("title"));
        Assert.Equal(new[] { "Edit", "Delete" }, cells[6].FindElements(By.TagName("button")).Select(button => button.Text).ToArray());
        // Status and Delete confirmation controls are never used to persist a mutation.
    }

    private IWebElement Row(JsonElement item) => ui.Browser.Driver.FindElements(Rows).Single(row =>
        row.Displayed && row.FindElement(By.CssSelector("td:first-child > div:first-child")).Text == item.ListingName());

    public void OpenEdit(JsonElement item)
    {
        Reset();
        Row(item).FindElements(By.CssSelector(".pd-row-actions button")).Single(button => button.Text == "Edit").Click();
        ui.Browser.Visible(Form);
        ProviderUi.EqualText(dashboard.ListingType switch {
            "Accommodation" => "Edit Accommodation Listing", "Restaurant" => "Edit Dining Listing", _ => "Edit Experience Listing"
        }, ui.Browser.Visible(By.CssSelector(".pd-modal__title")).Text);
    }

    private void Value(string id, string expected)
    {
        var input = ui.Browser.Visible(By.Id(id));
        var actual = input.GetDomProperty("value");
        Assert.NotNull(actual);
        if (input.TagName.Equals("textarea", StringComparison.OrdinalIgnoreCase))
        {
            // Selenium on Windows can return CRLF for LF text received from the API.
            // Preserve spaces, blank lines and text; normalize only line-ending format.
            expected = expected.Replace("\r\n", "\n").Replace('\r', '\n');
            actual = actual.Replace("\r\n", "\n").Replace('\r', '\n');
        }
        Assert.Equal(expected, actual);
    }
    private void Fill(string id, string value) => ui.Browser.Fill(By.Id(id), value);
    private static string Default(JsonElement item, string key, string fallback) => item.Text(key) is { Length: > 0 } value ? value : fallback;
    private static string DateValue(JsonElement item, string key) => item.Text(key) is { Length: >= 10 } value ? value[..10] : "";

    public void AssertEditValues(JsonElement item)
    {
        Value(NameId, item.ListingName());
        Value(Prefix + "-location", item.Text("location"));
        Value(Prefix + "-desc", item.Text("description"));
        Value(Prefix + "-price", ProviderUi.Number(item.Money(dashboard.ListingType switch {
            "Accommodation" => "pricePerNight", "Restaurant" => "pricePerPerson", _ => "price"
        })));
        Assert.Equal(item.Flag("isActive"), ui.Browser.Visible(By.CssSelector(".pd-modal__form input[name='isActive']")).Selected);
        if (dashboard.ListingType == "Accommodation")
        {
            foreach (var (id, key) in new[] {
                ("hotel-prop","propertyType"), ("hotel-bed","bedDetails"), ("hotel-amenities","amenities"), ("hotel-bath","bathroomDetails")
            }) Value(id, item.Text(key));
            Value("hotel-guests", item.Int("maxGuests") == 0 ? "2" : item.Int("maxGuests").ToString(CultureInfo.InvariantCulture));
            Value("hotel-minstay", item.Int("minStayNights") == 0 ? "1" : item.Int("minStayNights").ToString(CultureInfo.InvariantCulture));
        }
        else if (dashboard.ListingType == "Restaurant")
        {
            Value("rest-cuisine", item.Text("cuisineType"));
            Value("rest-style", Default(item, "diningStyle", "Casual Dining"));
            Value("rest-range", Default(item, "priceRange", "Moderate"));
            Value("rest-group-size", Default(item, "groupSizeCategory", "Table for Two"));
            Value("rest-seats", item.Int("seatingCapacity").ToString(CultureInfo.InvariantCulture));
            Value("rest-menu", item.Text("setMenuDetails"));
            Value("rest-diet", Default(item, "dietaryOptions", "Standard"));
            var hours = item.Text("openingHours").Split(" - ", StringSplitOptions.TrimEntries);
            var opening = hours.Length == 2 ? InputTime(hours[0]) : "11:30";
            var closing = hours.Length == 2 ? InputTime(hours[1]) : "22:00";
            Value("rest-hours-open", opening);
            Value("rest-hours-close", closing);
            AssertSlots(item, opening, "1 Hour");
        }
        else
        {
            Value("exp-unit", Default(item, "unit", "Per Person"));
            Value("exp-max", item.Int("maxParticipants") == 0 ? "10" : item.Int("maxParticipants").ToString(CultureInfo.InvariantCulture));
            Value("exp-duration", Default(item, "duration", "2 Hours"));
            Value("exp-days", item.Text("availableDays"));
            Value("exp-valid-from", DateValue(item, "validFrom"));
            Value("exp-valid-until", DateValue(item, "validUntil"));
            AssertSlots(item, "08:00", Default(item, "duration", "2 Hours"));
        }
        AssertNoFormError();
    }

    private static string InputTime(string value)
    {
        Assert.True(TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var time),
            "Existing provider time must be a parseable 12-hour or 24-hour value.");
        return time.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private static string AddDuration(string start, string duration)
    {
        var match = Regex.Match(duration, @"\d+(?:\.\d+)?");
        var number = match.Success ? double.Parse(match.Value, CultureInfo.InvariantCulture) : 0;
        var minutes = duration.Contains("hour", StringComparison.OrdinalIgnoreCase) ? number * 60 :
            duration.Contains("min", StringComparison.OrdinalIgnoreCase) ? number : 120;
        return TimeOnly.ParseExact(start, "HH:mm", CultureInfo.InvariantCulture)
            .AddMinutes(Math.Round(minutes, MidpointRounding.AwayFromZero)).ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private void AssertSlots(JsonElement item, string fallbackStart, string duration)
    {
        var slots = new List<(string Start, string End)>();
        foreach (var raw in item.Text("timeSlots").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = raw.Split(" - ", StringSplitOptions.TrimEntries);
            if (parts.Length == 2) slots.Add((InputTime(parts[0]), InputTime(parts[1])));
            else if (dashboard.ListingType == "Experience")
            {
                var start = InputTime(raw);
                slots.Add((start, AddDuration(start, duration)));
            }
        }
        if (slots.Count == 0) slots.Add((fallbackStart, AddDuration(fallbackStart, duration)));
        var inputs = ui.Browser.Driver.FindElements(By.CssSelector(".pd-modal__form .pd-slot-card input[type='time']"));
        Assert.Equal(slots.Count * 2, inputs.Count);
        for (var index = 0; index < slots.Count; index++)
        {
            Assert.Equal(slots[index].Start, inputs[index * 2].GetDomProperty("value"));
            Assert.Equal(slots[index].End, inputs[index * 2 + 1].GetDomProperty("value"));
        }
    }

    private void SetTime(IWebElement input, string value)
    {
        ui.Browser.Script.ExecuteScript("""
            Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set.call(arguments[0],arguments[1]);
            arguments[0].dispatchEvent(new Event('input',{bubbles:true}));
            arguments[0].dispatchEvent(new Event('change',{bubbles:true}));
            """, input, value);
        ui.Browser.Until(_ => input.GetDomProperty("value") == value, "Time input did not retain its draft value.");
    }

    public void PrepareValidDraftAndCancel()
    {
        Reset();
        ui.Browser.Click(By.Id("add-listing-btn"));
        ui.Browser.Visible(Form);
        var name = "CQ-E2E-DRAFT-" + Guid.NewGuid().ToString("N")[..8];
        var values = new Dictionary<string, string> {
            [NameId] = name, [Prefix + "-location"] = "Colombo",
            [Prefix + "-desc"] = "Valid catalog form draft; canceled without creating a listing.",
            [Prefix + "-price"] = "12000"
        };
        if (dashboard.ListingType == "Accommodation")
        {
            new SelectElement(ui.Browser.Visible(By.Id("hotel-prop"))).SelectByText("Guesthouse");
            Value("hotel-prop", "Guesthouse");
            values["hotel-guests"] = "2"; values["hotel-minstay"] = "1"; values["hotel-bed"] = "1 King Bed";
            values["hotel-amenities"] = "WiFi, Breakfast"; values["hotel-bath"] = "Private Bathroom";
        }
        else if (dashboard.ListingType == "Restaurant")
        {
            values["rest-cuisine"] = "Sri Lankan"; values["rest-seats"] = "20";
            values["rest-menu"] = "Three course Sri Lankan menu"; values["rest-diet"] = "Vegetarian options";
            new SelectElement(ui.Browser.Visible(By.Id("rest-style"))).SelectByText("Casual Dining");
            new SelectElement(ui.Browser.Visible(By.Id("rest-range"))).SelectByValue("Moderate");
            new SelectElement(ui.Browser.Visible(By.Id("rest-group-size"))).SelectByValue("Table for Two");
            Value("rest-style", "Casual Dining");
            Value("rest-range", "Moderate");
            Value("rest-group-size", "Table for Two");
            SetTime(ui.Browser.Visible(By.Id("rest-hours-open")), "09:00");
            SetTime(ui.Browser.Visible(By.Id("rest-hours-close")), "22:00");
        }
        else
        {
            values["exp-max"] = "10"; values["exp-duration"] = "2 Hours"; values["exp-days"] = "Daily";
            new SelectElement(ui.Browser.Visible(By.Id("exp-unit"))).SelectByValue("Per Person");
            Value("exp-unit", "Per Person");
            var tomorrow = TestSettings.TodayInSriLanka.AddDays(1);
            ui.Browser.SetDate(By.Id("exp-valid-from"), tomorrow);
            ui.Browser.SetDate(By.Id("exp-valid-until"), tomorrow.AddDays(30));
            Value("exp-valid-from", tomorrow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Value("exp-valid-until", tomorrow.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
        foreach (var (id, value) in values) Fill(id, value);
        foreach (var (id, value) in values) Value(id, value);
        if (dashboard.ListingType != "Accommodation")
        {
            var inputs = ui.Browser.Driver.FindElements(By.CssSelector(".pd-modal__form .pd-slot-card input[type='time']"));
            Assert.Equal(2, inputs.Count);
            SetTime(inputs[0], dashboard.ListingType == "Restaurant" ? "09:00" : "08:00");
            SetTime(inputs[1], "10:00");
        }
        Assert.True(ui.Browser.Visible(By.CssSelector(".pd-modal__form input[name='isActive']")).Selected);
        Assert.True(ui.Browser.Visible(By.CssSelector(".pd-modal__form button[type='submit']")).Enabled);
        AssertNoFormError();
        ui.Browser.Capture("valid-listing-draft");
        CancelForm();
        AssertRows(dashboard.Listings);
        Assert.DoesNotContain(ui.Browser.Driver.FindElements(Rows), row => row.Text.Contains(name, StringComparison.Ordinal));
        ui.Browser.Click(By.Id("add-listing-btn"));
        ui.Browser.Visible(Form);
        foreach (var id in new[] { NameId, Prefix + "-location", Prefix + "-desc", Prefix + "-price" }) Value(id, "");
        CancelForm();
        ui.AssertNoBusinessWrites();
    }

    public void ChangeAndCancelEdit(JsonElement item)
    {
        OpenEdit(item);
        AssertEditValues(item);
        var draftName = "CQ-E2E-CANCEL-" + Guid.NewGuid().ToString("N")[..8];
        Fill(NameId, draftName);
        Fill(Prefix + "-desc", "Valid local edit that is canceled without saving.");
        Value(NameId, draftName);
        ui.Browser.Capture("unsaved-listing-edit");
        CancelForm();
        AssertRows(dashboard.Listings);
        OpenEdit(item);
        AssertEditValues(item);
        CancelForm();
        ui.AssertNoBusinessWrites();
    }

    public void CancelDelete(JsonElement item)
    {
        Reset();
        Row(item).FindElements(By.CssSelector(".pd-row-actions button")).Single(button => button.Text == "Delete").Click();
        var dialog = ui.Browser.Visible(By.CssSelector(".cq-confirm-modal[role='dialog']"));
        ProviderUi.EqualText("Delete Listing", dialog.FindElement(By.CssSelector(".cq-confirm-title")).Text);
        Assert.Contains("Are you sure you want to delete this listing?", dialog.Text);
        Assert.True(dialog.FindElement(By.CssSelector(".cq-confirm-btn--danger")).Enabled);
        ui.Browser.Capture("delete-confirmation-before-cancel");
        ui.Browser.Click(By.CssSelector(".cq-confirm-btn--cancel"));
        ui.Browser.Until(driver => !driver.FindElements(By.CssSelector(".cq-confirm-modal")).Any(element => element.Displayed),
            "Delete confirmation did not close.");
        AssertRows(dashboard.Listings);
        AssertRow(Row(item), item);
        ui.AssertNoBusinessWrites();
    }

    public void CancelForm()
    {
        ui.Browser.Click(By.CssSelector(".pd-modal__form .pd-cancel-btn"));
        ui.Browser.Until(driver => !driver.FindElements(Form).Any(element => element.Displayed), "Listing form did not close.");
    }

    private void AssertNoFormError() => Assert.DoesNotContain(
        ui.Browser.Driver.FindElements(By.CssSelector(".pd-modal__form .pd-form-error")), element => element.Displayed);
}
