using System;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages;

public class AccommodationListingsPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public AccommodationListingsPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
    }

    // =========================================================
    // ELEMENTS
    // =========================================================

    private IWebElement ServicesNavigation =>
        WaitForElement(By.Id("pd-nav-services"));

    private IWebElement AddListingButton =>
        WaitForElement(By.Id("add-listing-btn"));

    private IWebElement RoomTypeInput =>
        WaitForElement(By.Id("hotel-room"));

    private IWebElement PropertyTypeSelect =>
        WaitForElement(By.Id("hotel-prop"));

    private IWebElement LocationInput =>
        WaitForElement(By.Id("hotel-location"));

    private IWebElement DescriptionInput =>
        WaitForElement(By.Id("hotel-desc"));

    private IWebElement PriceInput =>
        WaitForElement(By.Id("hotel-price"));

    private IWebElement MaxGuestsInput =>
        WaitForElement(By.Id("hotel-guests"));

    private IWebElement MinStayInput =>
        WaitForElement(By.Id("hotel-minstay"));

    private IWebElement BedDetailsInput =>
        WaitForElement(By.Id("hotel-bed"));

    private IWebElement AmenitiesInput =>
        WaitForElement(By.Id("hotel-amenities"));

    private IWebElement BathroomDetailsInput =>
        WaitForElement(By.Id("hotel-bath"));

    // =========================================================
    // OPEN PAGE
    // =========================================================

    public void Open()
    {
        ServicesNavigation.Click();

        _wait.Until(driver =>
            driver.FindElements(
                    By.XPath("//h1[normalize-space()='Rooms and Accommodations']"))
                .Any(x => x.Displayed));
    }

    // =========================================================
    // CREATE FORM
    // =========================================================

    public void OpenCreateForm()
    {
        AddListingButton.Click();

        _wait.Until(driver =>
            driver.FindElements(
                    By.XPath("//h2[normalize-space()='Create New Accommodation']"))
                .Any(x => x.Displayed));
    }

    public void FillValidAccommodation(string roomType)
    {
        SetText(RoomTypeInput, roomType);

        // Manually confirmed working value
        SelectPropertyType("Luxury Resort");

        SetText(LocationInput, "Negombo Beach");

        SetText(
            DescriptionInput,
            "Comfortable hotel room created for Selenium QA testing.");

        SetText(PriceInput, "12500");
        SetText(MaxGuestsInput, "2");
        SetText(MinStayInput, "1");
        SetText(BedDetailsInput, "1 King Bed");

        SetText(
            AmenitiesInput,
            "WiFi, Breakfast, Air Conditioning");

        SetText(
            BathroomDetailsInput,
            "Private bathroom with hot water");
    }

    private void SelectPropertyType(string propertyType)
    {
        var select = new SelectElement(PropertyTypeSelect);
        select.SelectByText(propertyType);

        _wait.Until(_ =>
        {
            var selectedText =
                new SelectElement(PropertyTypeSelect)
                    .SelectedOption
                    .Text
                    .Trim();

            return string.Equals(
                selectedText,
                propertyType,
                StringComparison.OrdinalIgnoreCase);
        });
    }

    // =========================================================
    // FIELD METHODS
    // =========================================================

    public void ClearRoomType()
    {
        var element = RoomTypeInput;

        element.Click();
        element.SendKeys(Keys.Control + "a");
        element.SendKeys(Keys.Backspace);

        _wait.Until(_ =>
            string.IsNullOrWhiteSpace(
                element.GetAttribute("value")));
    }

    public void SetDescription(string description)
    {
        SetText(
            DescriptionInput,
            description);
    }

    public void SetLocation(string location)
    {
        SetText(
            LocationInput,
            location);
    }

    public void SetAmenities(string amenities)
    {
        SetText(
            AmenitiesInput,
            amenities);
    }

    public void SetPrice(string price)
    {
        SetText(
            PriceInput,
            price);
    }

    // =========================================================
    // CREATE
    // =========================================================

    public void SubmitCreate()
    {
        var createButton =
            WaitForElement(
                By.XPath(
                    "//div[contains(@class,'pd-modal')]" +
                    "//button[@type='submit' and " +
                    "contains(normalize-space(),'Create New Accommodation')]"));

        ScrollTo(createButton);

        _wait.Until(_ =>
            createButton.Displayed &&
            createButton.Enabled);

        createButton.Click();

        WaitForSaveResult();
    }

    public void SubmitCreateWithoutWaitingForSuccess()
    {
        var createButton =
            WaitForElement(
                By.XPath(
                    "//div[contains(@class,'pd-modal')]" +
                    "//button[@type='submit' and " +
                    "contains(normalize-space(),'Create New Accommodation')]"));

        ScrollTo(createButton);

        _wait.Until(_ =>
            createButton.Displayed &&
            createButton.Enabled);

        createButton.Click();

        // Wait briefly for validation message to appear
        _wait.Until(driver =>
            HasFormError() ||
            !IsCreateFormStillOpen() ||
            driver.FindElements(By.CssSelector(".pd-form-error")).Count >= 0);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public void OpenEditForm(string roomType)
    {
        var row =
            FindListingRow(roomType)
            ?? throw new InvalidOperationException(
                $"Accommodation listing '{roomType}' was not found.");

        var editButton =
            row.FindElements(
                    By.XPath(".//button[normalize-space()='Edit']"))
                .FirstOrDefault(x =>
                    x.Displayed &&
                    x.Enabled);

        if (editButton == null)
        {
            throw new InvalidOperationException(
                $"Edit button for '{roomType}' was not found.");
        }

        ScrollTo(editButton);

        editButton.Click();

        _wait.Until(driver =>
            driver.FindElements(
                    By.XPath("//h2[normalize-space()='Edit Accommodation Listing']"))
                .Any(x => x.Displayed));
    }

    public void SubmitUpdate()
    {
        var saveButton =
            WaitForElement(
                By.XPath(
                    "//div[contains(@class,'pd-modal')]" +
                    "//button[@type='submit' and " +
                    "contains(normalize-space(),'Save Changes')]"));

        ScrollTo(saveButton);

        _wait.Until(_ =>
            saveButton.Displayed &&
            saveButton.Enabled);

        saveButton.Click();

        WaitForSaveResult();
    }

    // =========================================================
    // DELETE
    // =========================================================

    public void DeleteListing(string roomType)
    {
        var row =
            FindListingRow(roomType)
            ?? throw new InvalidOperationException(
                $"Accommodation listing '{roomType}' was not found.");

        var deleteButton =
            row.FindElements(
                    By.XPath(".//button[normalize-space()='Delete']"))
                .FirstOrDefault(x =>
                    x.Displayed &&
                    x.Enabled);

        if (deleteButton == null)
        {
            throw new InvalidOperationException(
                $"Delete button for '{roomType}' was not found.");
        }

        ScrollTo(deleteButton);

        deleteButton.Click();

        _wait.Until(driver =>
            driver.FindElements(
                    By.CssSelector(".cq-confirm-overlay"))
                .Any(x => x.Displayed));

        var confirmDeleteButton =
            _wait.Until(driver =>
                driver.FindElements(
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

        _wait.Until(driver =>
            !driver.FindElements(
                    By.CssSelector(".cq-confirm-overlay"))
                .Any(x => x.Displayed));
    }

    // =========================================================
    // LISTING CHECKS
    // =========================================================

    public bool ListingExists(string roomType)
    {
        return FindListingRow(roomType) != null;
    }

    public void WaitForListing(string roomType)
    {
        _wait.Until(_ =>
            ListingExists(roomType));
    }

    public void WaitUntilRemoved(string roomType)
    {
        _wait.Until(_ =>
            !ListingExists(roomType));
    }

    public string GetListingRowText(string roomType)
    {
        var row =
            FindListingRow(roomType)
            ?? throw new InvalidOperationException(
                $"Accommodation listing '{roomType}' was not found.");

        return row.Text;
    }

    private IWebElement? FindListingRow(string roomType)
    {
        var rows =
            _driver.FindElements(
                By.CssSelector(".pd-table tbody tr"));

        return rows.FirstOrDefault(row =>
            row.Displayed &&
            row.Text.Contains(
                roomType,
                StringComparison.OrdinalIgnoreCase));
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    public bool IsCreateFormStillOpen()
    {
        return _driver
            .FindElements(
                By.XPath("//h2[normalize-space()='Create New Accommodation']"))
            .Any(x => x.Displayed);
    }

    public bool HasFormError()
    {
        return FindVisibleFormErrors().Any();
    }

    public string GetFormError()
    {
        var error =
            FindVisibleFormErrors()
                .FirstOrDefault();

        return error?.Text?.Trim()
               ?? string.Empty;
    }

    public bool WaitForFormError()
    {
        try
        {
            return _wait.Until(_ =>
                HasFormError());
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    private System.Collections.Generic.IEnumerable<IWebElement>
        FindVisibleFormErrors()
    {
        var selectors = new[]
        {
            ".pd-form-error",
            ".pd-error",
            ".error-message",
            ".alert-danger"
        };

        return selectors
            .SelectMany(selector =>
                _driver.FindElements(
                    By.CssSelector(selector)))
            .Where(x =>
                x.Displayed &&
                !string.IsNullOrWhiteSpace(x.Text));
    }

    // =========================================================
    // SAVE RESULT
    // =========================================================

    private void WaitForSaveResult()
    {
        _wait.Until(driver =>
        {
            var errors =
                FindVisibleFormErrors()
                    .Select(x => x.Text.Trim())
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Accommodation form save failed: " +
                    string.Join(
                        " | ",
                        errors));
            }

            var modals =
                driver.FindElements(
                    By.CssSelector(".pd-modal"));

            return modals.Count == 0 ||
                   modals.All(x =>
                       !x.Displayed);
        });
    }

    // =========================================================
    // COMMON HELPERS
    // =========================================================

    private IWebElement WaitForElement(By locator)
    {
        var element =
            _wait.Until(driver =>
                driver.FindElements(locator)
                    .FirstOrDefault(x =>
                        x.Displayed &&
                        x.Enabled));

        return element
               ?? throw new InvalidOperationException(
                   $"Element was not found: {locator}");
    }

    private static void SetText(
        IWebElement element,
        string value)
    {
        element.Click();

        element.SendKeys(
            Keys.Control + "a");

        element.SendKeys(
            Keys.Backspace);

        element.SendKeys(value);
    }

    private void ScrollTo(IWebElement element)
    {
        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});",
                element);
    }
}