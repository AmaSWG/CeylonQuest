using System;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages;

public class RestaurantListingsPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public RestaurantListingsPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
    }

    // =====================================================
    // NAVIGATION
    // =====================================================

    private IWebElement ServicesNavigation =>
        _wait.Until(d =>
            d.FindElement(By.Id("pd-nav-services")));

    private IWebElement AddListingButton =>
        _wait.Until(d =>
            d.FindElement(By.Id("add-listing-btn")));

    // =====================================================
    // RESTAURANT FORM FIELDS
    // =====================================================

    private IWebElement NameInput =>
        _wait.Until(d =>
            d.FindElement(By.Id("rest-name")));

    private IWebElement CuisineInput =>
        _wait.Until(d =>
            d.FindElement(By.Id("rest-cuisine")));

    private IWebElement DiningStyleSelect =>
        _wait.Until(d =>
            d.FindElement(By.Id("rest-style")));

    private IWebElement LocationInput =>
        _wait.Until(d =>
            d.FindElement(By.Id("rest-location")));

    private IWebElement DescriptionInput =>
        _wait.Until(d =>
            d.FindElement(By.Id("rest-desc")));

    private IWebElement PriceInput =>
        _wait.Until(d =>
            d.FindElement(By.Id("rest-price")));

    private IWebElement PriceRangeSelect =>
        _wait.Until(d =>
            d.FindElement(By.Id("rest-range")));

    private IWebElement GroupSizeSelect =>
        _wait.Until(d =>
            d.FindElement(By.Id("rest-group-size")));

    private IWebElement OpeningTimeInput =>
        _wait.Until(d =>
            d.FindElement(By.Id("rest-hours-open")));

    private IWebElement ClosingTimeInput =>
        _wait.Until(d =>
            d.FindElement(By.Id("rest-hours-close")));

    private IWebElement MenuDetailsInput =>
        _wait.Until(d =>
            d.FindElement(By.Id("rest-menu")));

    private IWebElement DietaryOptionsInput =>
        _wait.Until(d =>
            d.FindElement(By.Id("rest-diet")));

    // =====================================================
    // OPEN PAGE
    // =====================================================

    public void Open()
    {
        ServicesNavigation.Click();

        _wait.Until(d =>
            d.FindElements(
                    By.XPath("//h1[normalize-space()='Menu and Dining']"))
                .Any(e => e.Displayed));
    }

    // =====================================================
    // OPEN CREATE FORM
    // =====================================================

    public void OpenCreateForm()
    {
        AddListingButton.Click();

        _wait.Until(d =>
            d.FindElements(
                    By.XPath(
                        "//h2[normalize-space()='Create New Dining Listing']"))
                .Any(e => e.Displayed));
    }

    // =====================================================
    // FILL VALID RESTAURANT
    // =====================================================

    public void FillValidRestaurant(string restaurantName)
    {
        SetText(
            NameInput,
            restaurantName);

        SetText(
            CuisineInput,
            "Sri Lankan Cuisine");

        // Explicitly select Dining Style
        // so React receives the actual value.
        SelectByText(
            DiningStyleSelect,
            "Fine Dining");

        SetText(
            LocationInput,
            "Negombo City");

        SetText(
            DescriptionInput,
            "This restaurant listing was created for Selenium QA testing.");

        SetText(
            PriceInput,
            "3500");

        SelectByText(
            PriceRangeSelect,
            "Moderate");

        SelectByText(
            GroupSizeSelect,
            "Table for Two");

        SetTime(
            OpeningTimeInput,
            "09:00");

        SetTime(
            ClosingTimeInput,
            "22:00");

        SetText(
            MenuDetailsInput,
            "Traditional Sri Lankan lunch and dinner menu.");

        SetText(
            DietaryOptionsInput,
            "Vegetarian options available.");
    }

    // =====================================================
    // VALIDATION HELPERS
    // =====================================================

    public void ClearRestaurantName()
    {
        NameInput.Clear();
    }

    public void SetRestaurantName(string name)
    {
        SetText(
            NameInput,
            name);
    }

    // =====================================================
    // UPDATE OPENING HOURS
    // =====================================================

    public void SetOpeningHours(
        string openingTime,
        string closingTime)
    {
        SetTime(
            OpeningTimeInput,
            openingTime);

        SetTime(
            ClosingTimeInput,
            closingTime);
    }

    // =====================================================
    // UPDATE PRICE RANGE
    // =====================================================

    public void SelectPriceRange(string priceRange)
    {
        SelectByText(
            PriceRangeSelect,
            priceRange);
    }

    // =====================================================
    // CREATE
    // =====================================================

    public void SubmitCreate()
    {
        var createButton =
            _wait.Until(d =>
                d.FindElement(
                    By.XPath(
                        "//div[contains(@class,'pd-modal')]" +
                        "//button[@type='submit' and " +
                        "contains(normalize-space()," +
                        "'Create New Dining Listing')]")));

        _wait.Until(_ =>
            createButton.Displayed &&
            createButton.Enabled);

        createButton.Click();

        WaitForSaveResult();
    }

    // =====================================================
    // CREATE WITHOUT EXPECTING SUCCESS
    // Used by TC57-09 and TC57-10
    // =====================================================

    public void SubmitCreateWithoutWaitingForSuccess()
    {
        var createButton =
            _wait.Until(d =>
                d.FindElement(
                    By.XPath(
                        "//div[contains(@class,'pd-modal')]" +
                        "//button[@type='submit' and " +
                        "contains(normalize-space()," +
                        "'Create New Dining Listing')]")));

        _wait.Until(_ =>
            createButton.Displayed &&
            createButton.Enabled);

        createButton.Click();
    }

    // =====================================================
    // UPDATE
    // =====================================================

    public void SubmitUpdate()
    {
        var saveButton =
            _wait.Until(d =>
                d.FindElement(
                    By.XPath(
                        "//div[contains(@class,'pd-modal')]" +
                        "//button[@type='submit' and " +
                        "contains(normalize-space()," +
                        "'Save Changes')]")));

        _wait.Until(_ =>
            saveButton.Displayed &&
            saveButton.Enabled);

        saveButton.Click();

        WaitForSaveResult();
    }

    // =====================================================
    // FORM VALIDATION STATE
    // =====================================================

    public bool IsCreateFormStillOpen()
    {
        return _driver
            .FindElements(
                By.XPath(
                    "//h2[normalize-space()='Create New Dining Listing']"))
            .Any(x => x.Displayed);
    }

    public bool HasFormError()
    {
        return _driver
            .FindElements(
                By.CssSelector(".pd-form-error"))
            .Any(x =>
                x.Displayed &&
                !string.IsNullOrWhiteSpace(x.Text));
    }

    public string GetFormError()
    {
        var error =
            _driver
                .FindElements(
                    By.CssSelector(".pd-form-error"))
                .FirstOrDefault(x =>
                    x.Displayed &&
                    !string.IsNullOrWhiteSpace(x.Text));

        return error?.Text ?? string.Empty;
    }

    // =====================================================
    // LISTING CHECK
    // =====================================================

    public bool ListingExists(string restaurantName)
    {
        return FindListingRow(restaurantName) != null;
    }

    public void WaitForListing(string restaurantName)
    {
        _wait.Until(_ =>
            ListingExists(restaurantName));
    }

    public string GetListingRowText(string restaurantName)
    {
        var row =
            FindListingRow(restaurantName);

        if (row == null)
        {
            throw new InvalidOperationException(
                $"Restaurant listing '{restaurantName}' was not found.");
        }

        return row.Text;
    }

    // =====================================================
    // EDIT
    // =====================================================

    public void OpenEditForm(string restaurantName)
    {
        var row =
            FindListingRow(restaurantName);

        if (row == null)
        {
            throw new InvalidOperationException(
                $"Restaurant listing '{restaurantName}' was not found.");
        }

        var editButton =
            row.FindElement(
                By.XPath(
                    ".//button[normalize-space()='Edit']"));

        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});",
                editButton);

        _wait.Until(_ =>
            editButton.Displayed &&
            editButton.Enabled);

        editButton.Click();

        _wait.Until(d =>
            d.FindElements(
                    By.XPath(
                        "//h2[normalize-space()='Edit Dining Listing']"))
                .Any(e => e.Displayed));
    }

    // =====================================================
    // DELETE
    // =====================================================

    public void DeleteListing(string restaurantName)
    {
        var row =
            FindListingRow(restaurantName);

        if (row == null)
        {
            throw new InvalidOperationException(
                $"Restaurant listing '{restaurantName}' was not found.");
        }

        var deleteButton =
            row.FindElement(
                By.XPath(
                    ".//button[normalize-space()='Delete']"));

        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});",
                deleteButton);

        _wait.Until(_ =>
            deleteButton.Displayed &&
            deleteButton.Enabled);

        deleteButton.Click();

        _wait.Until(d =>
            d.FindElements(
                    By.CssSelector(".cq-confirm-overlay"))
                .Any(x => x.Displayed));

        var confirmDeleteButton =
            _wait.Until(d =>
                d.FindElements(
                        By.CssSelector(
                            ".cq-confirm-overlay .cq-confirm-btn--danger"))
                    .FirstOrDefault(x =>
                        x.Displayed &&
                        x.Enabled));

        if (confirmDeleteButton == null)
        {
            throw new InvalidOperationException(
                "Delete confirmation button was not found.");
        }

        confirmDeleteButton.Click();

        _wait.Until(d =>
            !d.FindElements(
                    By.CssSelector(".cq-confirm-overlay"))
                .Any(x => x.Displayed));
    }

    public void WaitUntilRemoved(string restaurantName)
    {
        _wait.Until(_ =>
            !ListingExists(
                restaurantName));
    }

    // =====================================================
    // FIND ROW
    // =====================================================

    private IWebElement? FindListingRow(
        string restaurantName)
    {
        var rows =
            _driver.FindElements(
                By.CssSelector(
                    ".pd-table tbody tr"));

        return rows.FirstOrDefault(row =>
            row.Text.Contains(
                restaurantName,
                StringComparison.OrdinalIgnoreCase));
    }

    // =====================================================
    // WAIT FOR SAVE RESULT
    // =====================================================

    private void WaitForSaveResult()
    {
        _wait.Until(d =>
        {
            var errors =
                d.FindElements(
                        By.CssSelector(".pd-form-error"))
                    .Where(e =>
                        e.Displayed &&
                        !string.IsNullOrWhiteSpace(e.Text))
                    .ToList();

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Restaurant form save failed: " +
                    errors[0].Text);
            }

            var modals =
                d.FindElements(
                    By.CssSelector(".pd-modal"));

            return modals.Count == 0 ||
                   modals.All(m => !m.Displayed);
        });
    }

    // =====================================================
    // HELPERS
    // =====================================================

    private static void SetText(
        IWebElement element,
        string value)
    {
        element.Clear();
        element.SendKeys(value);
    }

    private static void SelectByText(
        IWebElement element,
        string visibleText)
    {
        var select =
            new SelectElement(element);

        select.SelectByText(
            visibleText);
    }

    private void SetTime(
        IWebElement element,
        string value)
    {
        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(
                @"
                const element = arguments[0];
                const newValue = arguments[1];

                const setter =
                    Object.getOwnPropertyDescriptor(
                        HTMLInputElement.prototype,
                        'value'
                    ).set;

                setter.call(
                    element,
                    newValue
                );

                element.dispatchEvent(
                    new Event(
                        'input',
                        { bubbles: true }
                    )
                );

                element.dispatchEvent(
                    new Event(
                        'change',
                        { bubbles: true }
                    )
                );
                ",
                element,
                value);
    }
}