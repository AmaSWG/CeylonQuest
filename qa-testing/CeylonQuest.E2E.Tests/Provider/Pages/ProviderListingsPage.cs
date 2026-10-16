using System.Text.Json;
using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.E2E.Tests.Provider.Pages;

internal sealed class ProviderListingsPage(ProviderUi ui, ProviderDashboardPage dashboard)
{
    private readonly string ownedName = "CQ-E2E-" + Guid.NewGuid().ToString("N");
    private string? ownedId;
    private string currentOwnedName = "";
    private static readonly By Submit = By.CssSelector(".pd-modal__form button[type='submit']");

    public void Open()
    {
        dashboard.Navigate("services");
        ui.Browser.Visible(By.Id("add-listing-btn"));
        ProviderUi.EqualText(dashboard.ListingType switch {
            "Accommodation" => "Rooms and Accommodations", "Restaurant" => "Menu and Dining", _ => "Experience Listings"
        }, ui.Browser.Visible(By.CssSelector(".pd-activities-tab h1")).Text);
    }

    public void AssertOwnedListing(string id)
    {
        var item = Assert.Single(dashboard.Listings, listing => listing.Text("id") == id);
        ui.Browser.Fill(By.CssSelector(".pd-activities-tab .pd-search-input"), item.ListingName());
        var row = Row(item.ListingName());
        ProviderUi.EqualText(item.ListingName(), row.FindElement(By.CssSelector("td:first-child > div:first-child")).Text);
        Assert.Contains(item.Text("location"), row.Text, StringComparison.OrdinalIgnoreCase);
        Assert.True(row.FindElements(By.CssSelector(".pd-row-actions button")).Count >= 2);
    }

    public void AssertRequiredFieldValidation()
    {
        ui.Browser.Click(By.Id("add-listing-btn"));
        ui.Browser.Visible(By.CssSelector(".pd-modal__form"));
        ui.Browser.Click(Submit);
        var expected = dashboard.ListingType switch {
            "Accommodation" => "Room type is required.", "Restaurant" => "Restaurant / item name is required.", _ => "Experience title is required."
        };
        ProviderUi.EqualText(expected, ui.Browser.Visible(By.CssSelector(".pd-form-error")).Text);
        Assert.Equal(dashboard.Listings.Length, ui.Wait(dashboard.CatalogPath).Body.GetArrayLength());
        ui.Browser.Capture("required-listing-field");
        ui.Browser.Click(By.CssSelector(".pd-modal__form .pd-cancel-btn"));
    }

    public void ExerciseOwnedListingLifecycle()
    {
        ProviderSettings.AssertWriteTarget(dashboard.CatalogOriginUrl);
        Exception? testFailure = null;
        try
        {
            CreateOwned();
            AssertOwnedListing(ownedId!);
            EditOwned();
            DeleteOwned();
        }
        catch (Exception error)
        {
            ui.Browser.Capture("listing-before-cleanup");
            testFailure = error;
        }
        try
        {
            if (ownedId is not null) DeleteOwned();
        }
        catch (Exception cleanupError)
        {
            ui.Browser.Step("Cleanup failed for test-created listing " + ownedName + ". Review its ID before manual cleanup: " + ownedId);
            if (testFailure is not null) throw new AggregateException("Listing assertions and cleanup both failed.", testFailure, cleanupError);
            throw;
        }
        if (testFailure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(testFailure).Throw();
    }

    private void CreateOwned()
    {
        var previousIds = dashboard.Listings.Select(item => item.Text("id")).ToHashSet();
        ui.Browser.Click(By.Id("add-listing-btn"));
        ui.Browser.Visible(By.CssSelector(".pd-modal__form"));
        var prefix = dashboard.ListingType switch { "Accommodation" => "hotel", "Restaurant" => "rest", _ => "exp" };
        var nameField = dashboard.ListingType switch { "Accommodation" => "hotel-room", "Restaurant" => "rest-name", _ => "exp-title" };
        ui.Browser.Fill(By.Id(nameField), ownedName);
        ui.Browser.Fill(By.Id(prefix + "-location"), "Colombo");
        ui.Browser.Fill(By.Id(prefix + "-desc"), "Dedicated Selenium test listing; created inactive and removed by this test.");
        ui.Browser.Fill(By.Id(prefix + "-price"), "12000");
        if (dashboard.ListingType == "Accommodation")
        {
            new SelectElement(ui.Browser.Visible(By.Id("hotel-prop"))).SelectByText("Guesthouse");
            ui.Browser.Fill(By.Id("hotel-guests"), "2");
            ui.Browser.Fill(By.Id("hotel-minstay"), "1");
            ui.Browser.Fill(By.Id("hotel-bed"), "1 King Bed");
        }
        else if (dashboard.ListingType == "Restaurant")
        {
            ui.Browser.Fill(By.Id("rest-cuisine"), "Sri Lankan");
            ui.Browser.Fill(By.Id("rest-seats"), "2");
            // The real create form initializes 09:00-22:00 opening hours and a
            // 09:00-10:00 reservation slot. Explicitly verify these valid defaults.
            Assert.Equal("09:00", ui.Browser.Visible(By.Id("rest-hours-open")).GetDomProperty("value"));
            Assert.Equal("22:00", ui.Browser.Visible(By.Id("rest-hours-close")).GetDomProperty("value"));
            var slots = ui.Browser.Driver.FindElements(By.CssSelector("#rest-time-slots input[type='time']"));
            Assert.Equal(2, slots.Count);
            Assert.True(slots.All(input => !string.IsNullOrWhiteSpace(input.GetDomProperty("value"))));
        }
        else
        {
            ui.Browser.Fill(By.Id("exp-max"), "2");
            ui.Browser.Fill(By.Id("exp-duration"), "2 Hours");
            ui.Browser.Fill(By.Id("exp-days"), "Daily");
            var times = ui.Browser.Driver.FindElements(By.CssSelector(".pd-slot-card input[type='time']"));
            Assert.Equal(2, times.Count);
            SetTime(times[0], "08:00");
            SetTime(times[1], "10:00");
        }
        var active = ui.Browser.Visible(By.CssSelector(".pd-modal__form input[name='isActive']"));
        if (active.Selected) active.Click();
        Assert.False(active.Selected);
        var before = ui.Sequence;
        // A write is sent only by the actual form submit, with its validation intact.
        ui.Browser.Step("Submitting inactive test-owned listing: " + ownedName);
        ui.Browser.Click(Submit);
        WaitSaved();
        dashboard.RefreshListingSnapshot(before);
        var created = Assert.Single(dashboard.Listings, item => item.ListingName() == ownedName && !previousIds.Contains(item.Text("id")));
        ownedId = created.Text("id");
        currentOwnedName = ownedName;
        Assert.False(created.Flag("isActive"));
        ui.Browser.Step("Created inactive test-owned listing: " + ownedId + " / " + ownedName);
        ui.Browser.Capture("test-listing-created");
    }

    private void EditOwned()
    {
        ProviderSettings.AssertWriteTarget(dashboard.CatalogOriginUrl);
        AssertOwnership();
        ui.Browser.Fill(By.CssSelector(".pd-activities-tab .pd-search-input"), currentOwnedName);
        Row(currentOwnedName).FindElement(By.XPath(".//button[normalize-space()='Edit']")).Click();
        var updated = ownedName + " Updated";
        var nameField = dashboard.ListingType switch { "Accommodation" => "hotel-room", "Restaurant" => "rest-name", _ => "exp-title" };
        ui.Browser.Fill(By.Id(nameField), updated);
        var before = ui.Sequence;
        ui.Browser.Click(Submit);
        WaitSaved();
        dashboard.RefreshListingSnapshot(before);
        var saved = Assert.Single(dashboard.Listings, item => item.Text("id") == ownedId);
        Assert.Equal(updated, saved.ListingName());
        Assert.False(saved.Flag("isActive"));
        currentOwnedName = updated;
        AssertOwnedListing(ownedId!);
        ui.Browser.Capture("test-listing-edited");
    }

    private void DeleteOwned()
    {
        ProviderSettings.AssertWriteTarget(dashboard.CatalogOriginUrl);
        if (ui.Browser.Driver.FindElements(By.CssSelector(".pd-modal__form")).Count > 0)
            ui.Browser.Click(By.CssSelector(".pd-modal__form .pd-cancel-btn"));
        Open();
        var item = Assert.Single(dashboard.Listings, listing => listing.Text("id") == ownedId);
        AssertOwnership();
        currentOwnedName = item.ListingName();
        ui.Browser.Fill(By.CssSelector(".pd-activities-tab .pd-search-input"), currentOwnedName);
        Row(currentOwnedName).FindElement(By.CssSelector(".pd-row-btn--delete")).Click();
        ProviderUi.EqualText("Delete Listing", ui.Browser.Visible(By.CssSelector(".cq-confirm-title")).Text);
        var before = ui.Sequence;
        ui.Browser.Click(By.CssSelector(".cq-confirm-btn--danger"));
        dashboard.RefreshListingSnapshot(before);
        Assert.DoesNotContain(dashboard.Listings, listing => listing.Text("id") == ownedId);
        ui.Browser.Until(d => d.FindElements(By.CssSelector(".pd-activities-tab tbody tr"))
            .All(row => !row.Text.Contains(currentOwnedName, StringComparison.Ordinal)), "Deleted test listing still appears.");
        ui.Browser.Step("Deleted only this test's created listing: " + ownedId);
        ownedId = null;
        ui.Browser.Capture("test-listing-deleted");
    }

    private void AssertOwnership()
    {
        Assert.False(string.IsNullOrWhiteSpace(ownedId));
        var item = Assert.Single(dashboard.Listings, listing => listing.Text("id") == ownedId);
        Assert.True(item.ListingName() is var name && (name == ownedName || name == ownedName + " Updated"),
            "Refusing to edit/delete a listing not created by this test.");
    }

    private IWebElement Row(string title) => ui.Browser.Visible(By.XPath(
        $"//div[contains(@class,'pd-activities-tab')]//tbody/tr[td[1]/div[1][normalize-space()={ProviderUi.Literal(title)}]]"));
    private void WaitSaved() => ui.Browser.Until(d => {
        var error = d.FindElements(By.CssSelector(".pd-form-error")).FirstOrDefault(element => element.Displayed);
        Assert.True(error is null, "Listing save failed: " + error?.Text);
        return d.FindElements(By.CssSelector(".pd-modal__form")).Count == 0;
    }, "Listing modal did not close after saving.");
    private void SetTime(IWebElement input, string value) => ui.Browser.Script.ExecuteScript("""
        Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set.call(arguments[0],arguments[1]);
        arguments[0].dispatchEvent(new Event('input',{bubbles:true}));
        arguments[0].dispatchEvent(new Event('change',{bubbles:true}));
        """, input, value);
}
