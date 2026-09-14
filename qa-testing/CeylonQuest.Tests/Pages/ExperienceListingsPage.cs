using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Globalization;

namespace CeylonQuest.Tests.Pages;

public class ExperienceListingsPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public ExperienceListingsPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(
            driver,
            TimeSpan.FromSeconds(20));
    }

    // ============================================================
    // NAVIGATION
    // ============================================================

    private IWebElement ServicesNavigation =>
        WaitForVisibleAndEnabled(
            By.Id("pd-nav-services"));

    // ============================================================
    // CREATE EXPERIENCE BUTTON
    //
    // Supports:
    // 1. old ID: add-activity-btn
    // 2. current UI: Create New Experience
    // ============================================================

    private IWebElement AddExperienceButton =>
        _wait.Until(d =>
        {
            var byId =
                d.FindElements(
                        By.Id("add-activity-btn"))
                    .FirstOrDefault(
                        IsVisibleAndEnabled);

            if (byId != null)
                return byId;

            var byText =
                d.FindElements(
                        By.XPath(
                            "//button[" +
                            "contains(normalize-space(.),'Create New Experience')" +
                            " or contains(normalize-space(.),'New Experience')" +
                            "]"))
                    .FirstOrDefault(
                        IsVisibleAndEnabled);

            return byText;
        })!;

    // ============================================================
    // FORM FIELDS
    // ============================================================

    private IWebElement TitleInput =>
        WaitForVisible(
            By.Id("exp-title"));

    private IWebElement LocationInput =>
        WaitForVisible(
            By.Id("exp-location"));

    private IWebElement DescriptionInput =>
        WaitForVisible(
            By.Id("exp-desc"));

    private IWebElement PriceInput =>
        WaitForVisible(
            By.Id("exp-price"));

    private IWebElement PricingUnitSelect =>
        WaitForVisible(
            By.Id("exp-unit"));

    private IWebElement MaxGuestsInput =>
        WaitForVisible(
            By.Id("exp-max"));

    private IWebElement DurationInput =>
        WaitForVisible(
            By.Id("exp-duration"));

    private IWebElement AvailableDaysInput =>
        WaitForVisible(
            By.Id("exp-days"));

    // ============================================================
    // OPEN EXPERIENCE LISTINGS
    // ============================================================

    public void Open()
    {
        var services =
            ServicesNavigation;

        ScrollIntoView(
            services);

        SafeClick(
            services);

        WaitForExperienceListingsPage();
    }

    public void WaitForExperienceListingsPage()
    {
        // Wait for heading.
        _wait.Until(d =>
            d.FindElements(
                    By.XPath(
                        "//*[self::h1 or self::h2]" +
                        "[contains(normalize-space(.)," +
                        "'Experience Listings')]"))
                .Any(SafeDisplayed));

        // Wait until create button is also ready.
        _wait.Until(d =>
        {
            bool oldButton =
                d.FindElements(
                        By.Id("add-activity-btn"))
                    .Any(SafeDisplayed);

            bool newButton =
                d.FindElements(
                        By.XPath(
                            "//button[" +
                            "contains(normalize-space(.)," +
                            "'Create New Experience')" +
                            "]"))
                    .Any(SafeDisplayed);

            return oldButton ||
                   newButton;
        });
    }

    // ============================================================
    // OPEN CREATE FORM
    // ============================================================

    public void OpenCreateForm()
    {
        WaitForExperienceListingsPage();

        var button =
            AddExperienceButton;

        if (button == null)
        {
            throw new NoSuchElementException(
                "Create New Experience button was not found.");
        }

        ScrollIntoView(
            button);

        SafeClick(
            button);

        // Do not depend only on modal heading.
        // The form field is a stronger locator.
        _wait.Until(d =>
        {
            bool titleVisible =
                d.FindElements(
                        By.Id("exp-title"))
                    .Any(SafeDisplayed);

            bool modalVisible =
                d.FindElements(
                        By.CssSelector(".pd-modal"))
                    .Any(SafeDisplayed);

            return titleVisible &&
                   modalVisible;
        });
    }

    // ============================================================
    // FORM INPUT
    // ============================================================

    public void EnterTitle(
        string title)
    {
        SetText(
            TitleInput,
            title);
    }

    public void EnterLocation(
        string location)
    {
        SetText(
            LocationInput,
            location);
    }

    public void EnterDescription(
        string description)
    {
        SetText(
            DescriptionInput,
            description);
    }

    public void EnterPrice(
        decimal price)
    {
        SetText(
            PriceInput,
            price.ToString(
                CultureInfo.InvariantCulture));
    }

    public void SelectPricingUnit(
        string unit)
    {
        var select =
            new SelectElement(
                PricingUnitSelect);

        var option =
            select.Options
                .FirstOrDefault(o =>
                    string.Equals(
                        o.Text.Trim(),
                        unit,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    string.Equals(
                        o.GetAttribute("value"),
                        unit,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    o.Text.Contains(
                        unit,
                        StringComparison.OrdinalIgnoreCase));

        if (option == null)
        {
            throw new NoSuchElementException(
                $"Pricing unit '{unit}' was not found.");
        }

        option.Click();
    }

    public void EnterMaxGuests(
        int maxGuests)
    {
        SetText(
            MaxGuestsInput,
            maxGuests.ToString());
    }

    public void EnterDuration(
        string duration)
    {
        SetText(
            DurationInput,
            duration);
    }

    public void EnterAvailableDays(
        string days)
    {
        SetText(
            AvailableDaysInput,
            days);
    }

    // ============================================================
    // TIME SLOT
    // ============================================================

    public void SetFirstTimeSlot(
        string startTime,
        string endTime)
    {
        var modal =
            _wait.Until(d =>
                d.FindElements(
                        By.CssSelector(".pd-modal"))
                    .FirstOrDefault(
                        SafeDisplayed));

        if (modal == null)
        {
            throw new NoSuchElementException(
                "Experience form modal was not found.");
        }

        var timeInputs =
            modal.FindElements(
                By.CssSelector(
                    "input[type='time']"));

        if (timeInputs.Count < 2)
        {
            throw new InvalidOperationException(
                "Expected start and end time inputs.");
        }

        SetTimeValue(
            timeInputs[0],
            startTime);

        SetTimeValue(
            timeInputs[1],
            endTime);
    }

    private void SetTimeValue(
        IWebElement element,
        string value)
    {
        ScrollIntoView(
            element);

        // Important for React controlled inputs.
        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(
                @"
                const input = arguments[0];
                const newValue = arguments[1];

                const descriptor =
                    Object.getOwnPropertyDescriptor(
                        HTMLInputElement.prototype,
                        'value'
                    );

                if (descriptor &&
                    descriptor.set)
                {
                    descriptor.set.call(
                        input,
                        newValue
                    );
                }
                else
                {
                    input.value =
                        newValue;
                }

                input.dispatchEvent(
                    new Event(
                        'input',
                        { bubbles: true }
                    )
                );

                input.dispatchEvent(
                    new Event(
                        'change',
                        { bubbles: true }
                    )
                );

                input.dispatchEvent(
                    new Event(
                        'blur',
                        { bubbles: true }
                    )
                );
                ",
                element,
                value);
    }

    // ============================================================
    // COMPLETE VALID FORM
    // ============================================================

    public void FillValidExperience(
        string title,
        string description,
        string location,
        decimal price)
    {
        EnterTitle(
            title);

        EnterLocation(
            location);

        EnterDescription(
            description);

        EnterPrice(
            price);

        SelectPricingUnit(
            "Per Person");

        EnterMaxGuests(
            10);

        EnterDuration(
            "2 Hours");

        EnterAvailableDays(
            "Daily");

        SetFirstTimeSlot(
            "08:00",
            "10:00");
    }

    // ============================================================
    // CREATE / PUBLISH
    // ============================================================

    public void Publish()
    {
        var button =
            _wait.Until(d =>
                d.FindElements(
                        By.XPath(
                            "//div[contains(@class,'pd-modal')]" +
                            "//button[@type='submit']"))
                    .FirstOrDefault(b =>
                        IsVisibleAndEnabled(b)
                        &&
                        (
                            b.Text.Contains(
                                "Publish Experience",
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            b.Text.Contains(
                                "Create Experience",
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            b.Text.Contains(
                                "Publish",
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            b.Text.Contains(
                                "Create",
                                StringComparison.OrdinalIgnoreCase)
                        )));

        if (button == null)
        {
            throw new NoSuchElementException(
                "Create/Publish Experience button was not found.");
        }

        ScrollIntoView(
            button);

        SafeClick(
            button);

        // The result can be:
        // - success toast
        // - modal closes
        // - validation remains in form
        _wait.Until(d =>
        {
            bool success =
                d.FindElements(
                        By.XPath(
                            "//*[contains(" +
                            "normalize-space(.)," +
                            "'Listing created')]"))
                    .Any(SafeDisplayed);

            bool modalClosed =
                !d.FindElements(
                        By.CssSelector(".pd-modal"))
                    .Any(SafeDisplayed);

            bool formError =
                d.FindElements(
                        By.CssSelector(".pd-form-error"))
                    .Any(SafeDisplayed);

            bool htmlInvalid =
                d.FindElements(
                        By.CssSelector(
                            "input:invalid," +
                            "textarea:invalid," +
                            "select:invalid"))
                    .Any();

            return success ||
                   modalClosed ||
                   formError ||
                   htmlInvalid;
        });
    }

    // ============================================================
    // SAVE UPDATE
    // ============================================================

    public void SaveChanges()
    {
        var button =
            _wait.Until(d =>
                d.FindElements(
                        By.XPath(
                            "//div[contains(@class,'pd-modal')]" +
                            "//button[@type='submit']"))
                    .FirstOrDefault(b =>
                        IsVisibleAndEnabled(b)
                        &&
                        (
                            b.Text.Contains(
                                "Save Changes",
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            b.Text.Contains(
                                "Update",
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            b.Text.Contains(
                                "Save",
                                StringComparison.OrdinalIgnoreCase)
                        )));

        if (button == null)
        {
            throw new NoSuchElementException(
                "Save Changes button was not found.");
        }

        ScrollIntoView(
            button);

        SafeClick(
            button);

        _wait.Until(d =>
        {
            bool modalClosed =
                !d.FindElements(
                        By.CssSelector(".pd-modal"))
                    .Any(SafeDisplayed);

            bool success =
                d.FindElements(
                        By.XPath(
                            "//*[contains(" +
                            "translate(normalize-space(.)," +
                            "'ABCDEFGHIJKLMNOPQRSTUVWXYZ'," +
                            "'abcdefghijklmnopqrstuvwxyz')," +
                            "'updated')]"))
                    .Any(SafeDisplayed);

            return modalClosed ||
                   success;
        });
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    public string WaitForFormError()
    {
        return _wait.Until(d =>
        {
            // Application validation messages.
            var error =
                d.FindElements(
                        By.CssSelector(
                            ".pd-form-error"))
                    .FirstOrDefault(e =>
                        SafeDisplayed(e)
                        &&
                        !string.IsNullOrWhiteSpace(
                            e.Text));

            if (error != null)
                return error.Text;

            // Browser validation fallback.
            var invalidInputs =
                d.FindElements(
                    By.CssSelector(
                        "input:invalid," +
                        "textarea:invalid," +
                        "select:invalid"));

            foreach (var invalid in invalidInputs)
            {
                var id =
                    invalid.GetAttribute("id")
                    ?? string.Empty;

                var message =
                    invalid.GetAttribute(
                        "validationMessage")
                    ?? string.Empty;

                if (id.Contains(
                        "title",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return $"Title: {message}";
                }

                if (id.Contains(
                        "price",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return $"Price: {message}";
                }

                if (!string.IsNullOrWhiteSpace(
                        message))
                {
                    return message;
                }
            }

            // Generic fallback.
            var genericError =
                d.FindElements(
                        By.XPath(
                            "//*[" +
                            "contains(@class,'error')" +
                            " or contains(@class,'invalid')" +
                            "]"))
                    .FirstOrDefault(e =>
                        SafeDisplayed(e)
                        &&
                        !string.IsNullOrWhiteSpace(
                            e.Text));

            return genericError?.Text;
        })!;
    }

    // ============================================================
    // LISTING LOOKUP
    // ============================================================

    public bool ListingExists(
        string title)
    {
        return FindListingRow(
                   title)
               != null;
    }

    public string GetListingRowText(
        string title)
    {
        var row =
            FindListingRow(
                title);

        if (row == null)
        {
            throw new InvalidOperationException(
                $"Listing '{title}' was not found.");
        }

        return row.Text;
    }

    public void WaitForListing(
        string title)
    {
        _wait.Until(_ =>
            ListingExists(
                title));
    }

    private IWebElement? FindListingRow(
        string title)
    {
        var rows =
            _driver.FindElements(
                By.CssSelector(
                    ".pd-table tbody tr"));

        var row =
            rows.FirstOrDefault(r =>
                SafeDisplayed(r)
                &&
                r.Text.Contains(
                    title,
                    StringComparison.OrdinalIgnoreCase));

        if (row != null)
            return row;

        // Fallback if frontend class changes
        // but table structure stays.
        rows =
            _driver.FindElements(
                By.CssSelector(
                    "table tbody tr"));

        return rows.FirstOrDefault(r =>
            SafeDisplayed(r)
            &&
            r.Text.Contains(
                title,
                StringComparison.OrdinalIgnoreCase));
    }

    // ============================================================
    // EDIT
    // ============================================================

    public void EditListing(
        string title)
    {
        var row =
            FindListingRow(
                title);

        if (row == null)
        {
            throw new InvalidOperationException(
                $"Listing '{title}' was not found.");
        }

        ScrollIntoView(
            row);

        var editButton =
            row.FindElements(
                    By.XPath(
                        ".//button[" +
                        "contains(normalize-space(.),'Edit')" +
                        "]"))
                .FirstOrDefault(
                    IsVisibleAndEnabled);

        if (editButton == null)
        {
            throw new NoSuchElementException(
                $"Edit button was not found for '{title}'.");
        }

        SafeClick(
            editButton);

        _wait.Until(d =>
        {
            bool modal =
                d.FindElements(
                        By.CssSelector(".pd-modal"))
                    .Any(SafeDisplayed);

            bool titleInput =
                d.FindElements(
                        By.Id("exp-title"))
                    .Any(SafeDisplayed);

            return modal &&
                   titleInput;
        });
    }

    // ============================================================
    // DELETE
    // ============================================================

    public void DeleteListing(
        string title)
    {
        var row =
            FindListingRow(
                title);

        if (row == null)
        {
            throw new InvalidOperationException(
                $"Listing '{title}' was not found.");
        }

        ScrollIntoView(
            row);

        var deleteButton =
            row.FindElements(
                    By.XPath(
                        ".//button[" +
                        "contains(normalize-space(.),'Delete')" +
                        "]"))
                .FirstOrDefault(
                    IsVisibleAndEnabled);

        if (deleteButton == null)
        {
            throw new NoSuchElementException(
                $"Delete button was not found for '{title}'.");
        }

        SafeClick(
            deleteButton);

        var confirmButton =
            _wait.Until(d =>
                d.FindElements(
                        By.XPath(
                            "//button[" +
                            "contains(normalize-space(.),'Delete Listing')" +
                            " or normalize-space(.)='Delete'" +
                            "]"))
                    .FirstOrDefault(
                        IsVisibleAndEnabled));

        if (confirmButton == null)
        {
            throw new NoSuchElementException(
                "Delete confirmation button was not found.");
        }

        SafeClick(
            confirmButton);
    }

    public void WaitUntilListingRemoved(
        string title)
    {
        _wait.Until(_ =>
            !ListingExists(
                title));
    }

    // ============================================================
    // SPA REFRESH
    // ============================================================

    public void RefreshAndReopen()
    {
        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(
                "window.history.replaceState({}, '', '/');");

        _driver.Navigate()
            .Refresh();

        _wait.Until(d =>
            d.FindElements(
                    By.Id("pd-nav-services"))
                .Any(SafeDisplayed));

        Open();
    }

    // ============================================================
    // HELPER METHODS
    // ============================================================

    private IWebElement WaitForVisible(
        By locator)
    {
        return _wait.Until(d =>
            d.FindElements(
                    locator)
                .FirstOrDefault(
                    SafeDisplayed))!;
    }

    private IWebElement WaitForVisibleAndEnabled(
        By locator)
    {
        return _wait.Until(d =>
            d.FindElements(
                    locator)
                .FirstOrDefault(
                    IsVisibleAndEnabled))!;
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

        if (!string.IsNullOrEmpty(
                value))
        {
            element.SendKeys(
                value);
        }
    }

    private void ScrollIntoView(
        IWebElement element)
    {
        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(
                "arguments[0].scrollIntoView(" +
                "{block:'center',inline:'center'});",
                element);
    }

    private void SafeClick(
        IWebElement element)
    {
        ScrollIntoView(
            element);

        _wait.Until(_ =>
            SafeDisplayed(element)
            &&
            SafeEnabled(element));

        try
        {
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            JavaScriptClick(
                element);
        }
        catch (WebDriverException)
        {
            JavaScriptClick(
                element);
        }
    }

    private void JavaScriptClick(
        IWebElement element)
    {
        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(
                "arguments[0].click();",
                element);
    }

    private static bool SafeDisplayed(
        IWebElement element)
    {
        try
        {
            return element.Displayed;
        }
        catch
        {
            return false;
        }
    }

    private static bool SafeEnabled(
        IWebElement element)
    {
        try
        {
            return element.Enabled;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsVisibleAndEnabled(
        IWebElement element)
    {
        return SafeDisplayed(element)
               &&
               SafeEnabled(element);
    }
}