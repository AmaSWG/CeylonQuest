using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages;

public class ExperienceListingsPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public ExperienceListingsPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
    }

    private IWebElement ServicesNavigation =>
        _wait.Until(d => d.FindElement(By.Id("pd-nav-services")));

    private IWebElement AddExperienceButton =>
        _wait.Until(d => d.FindElement(By.Id("add-activity-btn")));

    private IWebElement TitleInput =>
        _wait.Until(d => d.FindElement(By.Id("exp-title")));

    private IWebElement LocationInput =>
        _wait.Until(d => d.FindElement(By.Id("exp-location")));

    private IWebElement DescriptionInput =>
        _wait.Until(d => d.FindElement(By.Id("exp-desc")));

    private IWebElement PriceInput =>
        _wait.Until(d => d.FindElement(By.Id("exp-price")));

    private IWebElement PricingUnitSelect =>
        _wait.Until(d => d.FindElement(By.Id("exp-unit")));

    private IWebElement MaxGuestsInput =>
        _wait.Until(d => d.FindElement(By.Id("exp-max")));

    private IWebElement DurationInput =>
        _wait.Until(d => d.FindElement(By.Id("exp-duration")));

    private IWebElement AvailableDaysInput =>
        _wait.Until(d => d.FindElement(By.Id("exp-days")));

    public void Open()
    {
        ServicesNavigation.Click();
        WaitForExperienceListingsPage();
    }

    public void WaitForExperienceListingsPage()
    {
        _wait.Until(d =>
            d.FindElements(By.XPath("//h1[normalize-space()='Experience Listings']"))
             .Count > 0);
    }

    public void OpenCreateForm()
    {
        AddExperienceButton.Click();

        _wait.Until(d =>
            d.FindElements(By.XPath("//h2[normalize-space()='Create New Experience']"))
             .Count > 0);
    }

    public void EnterTitle(string title)
    {
        TitleInput.Clear();
        if (!string.IsNullOrWhiteSpace(title))
            TitleInput.SendKeys(title);
    }

    public void EnterLocation(string location)
    {
        LocationInput.Clear();
        LocationInput.SendKeys(location);
    }

    public void EnterDescription(string description)
    {
        DescriptionInput.Clear();
        DescriptionInput.SendKeys(description);
    }

    public void EnterPrice(decimal price)
    {
        PriceInput.Clear();
        PriceInput.SendKeys(price.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
    }

    public void SelectPricingUnit(string unit)
    {
        new SelectElement(PricingUnitSelect).SelectByText(unit);
    }

    public void EnterMaxGuests(int maxGuests)
    {
        MaxGuestsInput.Clear();
        MaxGuestsInput.SendKeys(maxGuests.ToString());
    }

    public void EnterDuration(string duration)
    {
        DurationInput.Clear();
        DurationInput.SendKeys(duration);
    }

    public void EnterAvailableDays(string days)
    {
        AvailableDaysInput.Clear();
        AvailableDaysInput.SendKeys(days);
    }

    public void SetFirstTimeSlot(string startTime, string endTime)
    {
        IWebElement modal = _wait.Until(d =>
            d.FindElement(By.CssSelector(".pd-modal")));

        var timeInputs = modal.FindElements(
            By.CssSelector("input[type='time']"));

        if (timeInputs.Count < 2)
        {
            throw new InvalidOperationException(
                "Expected start and end time inputs in the Experience modal.");
        }

        SetInputValue(timeInputs[0], startTime);
        SetInputValue(timeInputs[1], endTime);
    }

    private static void SetInputValue(IWebElement element, string value)
    {
        element.SendKeys(Keys.Control + "a");
        element.SendKeys(value);
    }

    public void FillValidExperience(
        string title,
        string description,
        string location,
        decimal price)
    {
        EnterTitle(title);
        EnterLocation(location);
        EnterDescription(description);
        EnterPrice(price);
        SelectPricingUnit("Per Person");
        EnterMaxGuests(10);
        EnterDuration("2 Hours");
        EnterAvailableDays("Daily");
        SetFirstTimeSlot("08:00", "10:00");
    }

    public void Publish()
    {
        var button = _wait.Until(d =>
            d.FindElement(By.XPath(
                "//div[contains(@class,'pd-modal')]//button[@type='submit' and contains(normalize-space(),'Publish Experience')]")));

        button.Click();
    }

    public void SaveChanges()
    {
        var button = _wait.Until(d =>
            d.FindElement(By.XPath(
                "//div[contains(@class,'pd-modal')]//button[@type='submit' and contains(normalize-space(),'Save Changes')]")));

        button.Click();
    }

    public string WaitForFormError()
    {
        return _wait.Until(d =>
        {
            var errors = d.FindElements(By.CssSelector(".pd-form-error"));

            return errors.Count > 0 &&
                   errors[0].Displayed &&
                   !string.IsNullOrWhiteSpace(errors[0].Text)
                ? errors[0].Text
                : null;
        })!;
    }

    public bool ListingExists(string title)
    {
        return FindListingRow(title) != null;
    }

    public string GetListingRowText(string title)
    {
        var row = FindListingRow(title)
            ?? throw new InvalidOperationException(
                $"Listing '{title}' was not found.");

        return row.Text;
    }

    public void WaitForListing(string title)
    {
        _wait.Until(_ => ListingExists(title));
    }

    private IWebElement? FindListingRow(string title)
    {
        var rows = _driver.FindElements(
            By.CssSelector(".pd-table tbody tr"));

        return rows.FirstOrDefault(row =>
            row.Text.Contains(title, StringComparison.OrdinalIgnoreCase));
    }

    public void EditListing(string title)
    {
        var row = FindListingRow(title)
            ?? throw new InvalidOperationException(
                $"Listing '{title}' was not found.");

        row.FindElement(By.XPath(
            ".//button[normalize-space()='Edit']")).Click();

        _wait.Until(d =>
            d.FindElements(By.XPath(
                "//h2[normalize-space()='Edit Experience Listing']"))
             .Count > 0);
    }

    public void DeleteListing(string title)
    {
        var row = FindListingRow(title)
            ?? throw new InvalidOperationException(
                $"Listing '{title}' was not found.");

        row.FindElement(By.XPath(
            ".//button[normalize-space()='Delete']")).Click();

        var confirmButton = _wait.Until(d =>
            d.FindElement(By.XPath(
                "//button[normalize-space()='Delete Listing']")));

        confirmButton.Click();
    }

    public void WaitUntilListingRemoved(string title)
    {
        _wait.Until(_ => !ListingExists(title));
    }

    // Important for this SPA:
    // after login the URL is still /login. If Selenium refreshes /login,
    // App.jsx opens the Login page again even though authToken exists.
    // Change URL to / first, then refresh. App.jsx will restore ProviderDashboard.
    public void RefreshAndReopen()
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "window.history.replaceState({}, '', '/');");

        _driver.Navigate().Refresh();

        _wait.Until(d =>
            d.FindElements(By.Id("pd-nav-services")).Count > 0);

        Open();
    }
}
