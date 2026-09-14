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

    private IWebElement AddListingButton =>
        _wait.Until(d =>
        {
            var byId =
                d.FindElements(
                        By.Id("add-listing-btn"))
                    .FirstOrDefault(
                        IsVisibleAndEnabled);

            if (byId != null)
                return byId;

            return d.FindElements(
                    By.XPath(
                        "//button[" +
                        "contains(normalize-space(.),'Create New Dining Listing')" +
                        " or contains(normalize-space(.),'New Dining Listing')" +
                        " or contains(normalize-space(.),'Add Restaurant')" +
                        "]"))
                .FirstOrDefault(
                    IsVisibleAndEnabled);
        })!;

    // ============================================================
    // FORM FIELDS
    // ============================================================

    private IWebElement NameInput =>
        WaitForVisible(
            By.Id("rest-name"));

    private IWebElement CuisineInput =>
        WaitForVisible(
            By.Id("rest-cuisine"));

    private IWebElement DiningStyleSelect =>
        WaitForVisible(
            By.Id("rest-style"));

    private IWebElement LocationInput =>
        WaitForVisible(
            By.Id("rest-location"));

    private IWebElement DescriptionInput =>
        WaitForVisible(
            By.Id("rest-desc"));

    private IWebElement PriceInput =>
        WaitForVisible(
            By.Id("rest-price"));

    private IWebElement PriceRangeSelect =>
        WaitForVisible(
            By.Id("rest-range"));

    private IWebElement GroupSizeSelect =>
        WaitForVisible(
            By.Id("rest-group-size"));

    private IWebElement OpeningTimeInput =>
        WaitForVisible(
            By.Id("rest-hours-open"));

    private IWebElement ClosingTimeInput =>
        WaitForVisible(
            By.Id("rest-hours-close"));

    private IWebElement MenuDetailsInput =>
        WaitForVisible(
            By.Id("rest-menu"));

    private IWebElement DietaryOptionsInput =>
        WaitForVisible(
            By.Id("rest-diet"));

    // ============================================================
    // OPEN PAGE
    // ============================================================

    public void Open()
    {
        var navigation =
            ServicesNavigation;

        ScrollIntoView(
            navigation);

        SafeClick(
            navigation);

        _wait.Until(d =>
        {
            bool addButtonVisible =
                d.FindElements(
                        By.Id("add-listing-btn"))
                    .Any(
                        SafeDisplayed);

            bool headingVisible =
                d.FindElements(
                        By.XPath(
                            "//*[self::h1 or self::h2 or self::h3][" +
                            "contains(" +
                            "translate(normalize-space(.)," +
                            "'ABCDEFGHIJKLMNOPQRSTUVWXYZ'," +
                            "'abcdefghijklmnopqrstuvwxyz')," +
                            "'restaurant')" +
                            " or contains(" +
                            "translate(normalize-space(.)," +
                            "'ABCDEFGHIJKLMNOPQRSTUVWXYZ'," +
                            "'abcdefghijklmnopqrstuvwxyz')," +
                            "'dining')" +
                            "]"))
                    .Any(
                        SafeDisplayed);

            return addButtonVisible
                   ||
                   headingVisible;
        });
    }

    // ============================================================
    // OPEN CREATE FORM
    // ============================================================

    public void OpenCreateForm()
    {
        var addButton =
            AddListingButton;

        if (addButton == null)
        {
            throw new NoSuchElementException(
                "Create New Dining Listing button was not found.");
        }

        ScrollIntoView(
            addButton);

        SafeClick(
            addButton);

        _wait.Until(d =>
        {
            bool nameInputVisible =
                d.FindElements(
                        By.Id("rest-name"))
                    .Any(
                        SafeDisplayed);

            bool formVisible =
                d.FindElements(
                        By.CssSelector("form"))
                    .Any(f =>
                    {
                        try
                        {
                            return f.Displayed
                                   &&
                                   f.FindElements(
                                           By.Id("rest-name"))
                                       .Count > 0;
                        }
                        catch
                        {
                            return false;
                        }
                    });

            return nameInputVisible
                   ||
                   formVisible;
        });
    }

    // ============================================================
    // FILL VALID RESTAURANT
    // ============================================================

    public void FillValidRestaurant(
        string restaurantName)
    {
        SetText(
            NameInput,
            restaurantName);

        SetText(
            CuisineInput,
            "Sri Lankan Cuisine");

        SelectOption(
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
            "3500.01");

        SelectOption(
            PriceRangeSelect,
            "Moderate");

        SelectOption(
            GroupSizeSelect,
            "Table for Two");

        SetOpeningHours(
            "09:00",
            "22:00");

        SetText(
            MenuDetailsInput,
            "Traditional Sri Lankan lunch and dinner menu.");

        SetText(
            DietaryOptionsInput,
            "Vegetarian options available.");
    }

    // ============================================================
    // NAME
    // ============================================================

    public void ClearRestaurantName()
    {
        ClearInput(
            NameInput);
    }

    public void SetRestaurantName(
        string name)
    {
        SetText(
            NameInput,
            name);
    }

    // ============================================================
    // OPENING HOURS
    // ============================================================

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

    public string GetOpeningTime()
    {
        return OpeningTimeInput
                   .GetAttribute("value")
               ?? string.Empty;
    }

    public string GetClosingTime()
    {
        return ClosingTimeInput
                   .GetAttribute("value")
               ?? string.Empty;
    }

    // ============================================================
    // PRICE RANGE
    // ============================================================

    public void SelectPriceRange(
        string priceRange)
    {
        SelectOption(
            PriceRangeSelect,
            priceRange);
    }

    // ============================================================
    // CREATE
    // ============================================================

    public void SubmitCreate()
    {
        var button =
            FindCreateSubmitButton();

        if (button == null)
        {
            throw new NoSuchElementException(
                "Create New Dining Listing button was not found.");
        }

        EnsureHtmlFormIsValid(
            button);

        ScrollIntoView(
            button);

        SafeClick(
            button);

        WaitForCreateResult();
    }

    public void SubmitCreateWithoutWaitingForSuccess()
    {
        var button =
            FindCreateSubmitButton();

        if (button == null)
        {
            throw new NoSuchElementException(
                "Create New Dining Listing button was not found.");
        }

        ScrollIntoView(
            button);

        SafeClick(
            button);
    }

    private IWebElement? FindCreateSubmitButton()
    {
        return _wait.Until(d =>
        {
            var buttons =
                d.FindElements(
                    By.CssSelector(
                        "button[type='submit']"));

            return buttons.FirstOrDefault(b =>
                IsVisibleAndEnabled(b)
                &&
                (
                    b.Text.Contains(
                        "Create New Dining Listing",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    b.Text.Contains(
                        "Create Dining Listing",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    b.Text.Contains(
                        "Create",
                        StringComparison.OrdinalIgnoreCase)
                ));
        });
    }

    // ============================================================
    // UPDATE
    // ============================================================

    public void SubmitUpdate()
    {
        var saveButton =
            _wait.Until(d =>
                d.FindElements(
                        By.CssSelector(
                            "button[type='submit']"))
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
                            b.Text.Equals(
                                "Save",
                                StringComparison.OrdinalIgnoreCase)
                        )));

        if (saveButton == null)
        {
            throw new NoSuchElementException(
                "Restaurant update button was not found.");
        }

        EnsureHtmlFormIsValid(
            saveButton);

        ScrollIntoView(
            saveButton);

        SafeClick(
            saveButton);

        WaitForSaveResult();
    }

    // ============================================================
    // VALIDATION STATE
    // ============================================================

    public bool IsCreateFormStillOpen()
    {
        return IsRestaurantFormVisible();
    }

    public bool HasFormError()
    {
        if (FindVisibleError() != null)
            return true;

        return _driver
            .FindElements(
                By.CssSelector(
                    "input:invalid," +
                    "select:invalid," +
                    "textarea:invalid"))
            .Any(
                SafeDisplayed);
    }

    public string GetFormError()
    {
        var custom =
            FindVisibleError();

        if (custom != null)
            return custom.Text;

        var invalid =
            _driver
                .FindElements(
                    By.CssSelector(
                        "input:invalid," +
                        "select:invalid," +
                        "textarea:invalid"))
                .FirstOrDefault(
                    SafeDisplayed);

        if (invalid == null)
            return string.Empty;

        try
        {
            var message =
                invalid.GetAttribute(
                    "validationMessage");

            if (!string.IsNullOrWhiteSpace(
                    message))
            {
                return message;
            }
        }
        catch
        {
        }

        return "Restaurant form contains an invalid field.";
    }

    // ============================================================
    // LISTING
    // ============================================================

    public bool ListingExists(
        string restaurantName)
    {
        return FindListingRow(
                   restaurantName)
               != null;
    }

    public void WaitForListing(
        string restaurantName)
    {
        _wait.Until(_ =>
            ListingExists(
                restaurantName));
    }

    public string GetListingRowText(
        string restaurantName)
    {
        var row =
            FindListingRow(
                restaurantName);

        if (row == null)
        {
            throw new InvalidOperationException(
                $"Restaurant listing '{restaurantName}' was not found.");
        }

        return row.Text;
    }

    private IWebElement? FindListingRow(
        string restaurantName)
    {
        var rows =
            _driver.FindElements(
                By.CssSelector(
                    ".pd-table tbody tr"));

        var result =
            rows.FirstOrDefault(r =>
                SafeDisplayed(r)
                &&
                r.Text.Contains(
                    restaurantName,
                    StringComparison.OrdinalIgnoreCase));

        if (result != null)
            return result;

        rows =
            _driver.FindElements(
                By.CssSelector(
                    "table tbody tr"));

        result =
            rows.FirstOrDefault(r =>
                SafeDisplayed(r)
                &&
                r.Text.Contains(
                    restaurantName,
                    StringComparison.OrdinalIgnoreCase));

        if (result != null)
            return result;

        var cards =
            _driver.FindElements(
                By.XPath(
                    "//*[" +
                    "contains(@class,'listing')" +
                    " or contains(@class,'card')" +
                    " or contains(@class,'service')" +
                    "]"));

        return cards.FirstOrDefault(c =>
            SafeDisplayed(c)
            &&
            c.Text.Contains(
                restaurantName,
                StringComparison.OrdinalIgnoreCase));
    }

    // ============================================================
    // EDIT
    // ============================================================

    public void OpenEditForm(
        string restaurantName)
    {
        var row =
            FindListingRow(
                restaurantName);

        if (row == null)
        {
            throw new InvalidOperationException(
                $"Restaurant listing '{restaurantName}' was not found.");
        }

        ScrollIntoView(
            row);

        var editButton =
            row.FindElements(
                    By.XPath(
                        ".//button[" +
                        "contains(" +
                        "translate(normalize-space(.)," +
                        "'ABCDEFGHIJKLMNOPQRSTUVWXYZ'," +
                        "'abcdefghijklmnopqrstuvwxyz')," +
                        "'edit')" +
                        "]"))
                .FirstOrDefault(
                    IsVisibleAndEnabled);

        if (editButton == null)
        {
            throw new NoSuchElementException(
                $"Edit button for '{restaurantName}' was not found.");
        }

        SafeClick(
            editButton);

        _wait.Until(d =>
            d.FindElements(
                    By.Id("rest-name"))
                .Any(
                    SafeDisplayed));
    }

    // ============================================================
    // DELETE
    // ============================================================

    public void DeleteListing(
        string restaurantName)
    {
        var row =
            FindListingRow(
                restaurantName);

        if (row == null)
        {
            throw new InvalidOperationException(
                $"Restaurant listing '{restaurantName}' was not found.");
        }

        ScrollIntoView(
            row);

        var deleteButton =
            row.FindElements(
                    By.XPath(
                        ".//button[" +
                        "contains(" +
                        "translate(normalize-space(.)," +
                        "'ABCDEFGHIJKLMNOPQRSTUVWXYZ'," +
                        "'abcdefghijklmnopqrstuvwxyz')," +
                        "'delete')" +
                        "]"))
                .FirstOrDefault(
                    IsVisibleAndEnabled);

        if (deleteButton == null)
        {
            throw new NoSuchElementException(
                $"Delete button for '{restaurantName}' was not found.");
        }

        SafeClick(
            deleteButton);

        var confirmButton =
            _wait.Until(d =>
            {
                var overlayButton =
                    d.FindElements(
                            By.CssSelector(
                                ".cq-confirm-overlay " +
                                ".cq-confirm-btn--danger"))
                        .FirstOrDefault(
                            IsVisibleAndEnabled);

                if (overlayButton != null)
                    return overlayButton;

                return d.FindElements(
                        By.XPath(
                            "//button[" +
                            "contains(normalize-space(.),'Delete Listing')" +
                            " or normalize-space(.)='Delete'" +
                            "]"))
                    .FirstOrDefault(
                        IsVisibleAndEnabled);
            });

        if (confirmButton == null)
        {
            throw new NoSuchElementException(
                "Delete confirmation button was not found.");
        }

        SafeClick(
            confirmButton);
    }

    public void WaitUntilRemoved(
        string restaurantName)
    {
        _wait.Until(_ =>
            !ListingExists(
                restaurantName));
    }

    // ============================================================
    // CREATE RESULT
    // FIXED: SUCCESS IS CHECKED BEFORE ERROR
    // ============================================================

    private void WaitForCreateResult()
    {
        try
        {
            _wait.Until(_ =>
            {
                if (HasVisibleSuccessMessage())
                    return true;

                if (!IsRestaurantFormVisible())
                    return true;

                var error =
                    FindVisibleError();

                if (error != null)
                {
                    throw new InvalidOperationException(
                        "Restaurant create failed: " +
                        error.Text);
                }

                return false;
            });
        }
        catch (WebDriverTimeoutException)
        {
            var diagnostic =
                GetInvalidFieldDiagnostic();

            if (!string.IsNullOrWhiteSpace(
                    diagnostic))
            {
                throw new InvalidOperationException(
                    "Restaurant form contains an invalid field: " +
                    diagnostic);
            }

            var error =
                FindVisibleError();

            if (error != null)
            {
                throw new InvalidOperationException(
                    "Restaurant create failed: " +
                    error.Text);
            }

            throw;
        }
    }

    // ============================================================
    // SAVE RESULT
    // FIXED: SUCCESS IS CHECKED BEFORE ERROR
    // ============================================================

    private void WaitForSaveResult()
    {
        try
        {
            _wait.Until(_ =>
            {
                if (HasVisibleSuccessMessage())
                    return true;

                if (!IsRestaurantFormVisible())
                    return true;

                var error =
                    FindVisibleError();

                if (error != null)
                {
                    throw new InvalidOperationException(
                        "Restaurant update failed: " +
                        error.Text);
                }

                return false;
            });
        }
        catch (WebDriverTimeoutException)
        {
            var diagnostic =
                GetInvalidFieldDiagnostic();

            if (!string.IsNullOrWhiteSpace(
                    diagnostic))
            {
                throw new InvalidOperationException(
                    "Restaurant form contains an invalid field: " +
                    diagnostic);
            }

            var error =
                FindVisibleError();

            if (error != null)
            {
                throw new InvalidOperationException(
                    "Restaurant update failed: " +
                    error.Text);
            }

            throw;
        }
    }

    // ============================================================
    // HTML VALIDATION
    // ============================================================

    private void EnsureHtmlFormIsValid(
        IWebElement submitButton)
    {
        var result =
            ((IJavaScriptExecutor)_driver)
            .ExecuteScript(
                @"
                const button = arguments[0];
                const form = button.closest('form');

                if (!form)
                    return true;

                return form.checkValidity();
                ",
                submitButton);

        if (result is bool valid &&
            valid)
        {
            return;
        }

        var diagnostic =
            GetInvalidFieldDiagnostic();

        throw new InvalidOperationException(
            "Restaurant form contains an invalid field. " +
            diagnostic);
    }

    private string GetInvalidFieldDiagnostic()
    {
        try
        {
            var result =
                ((IJavaScriptExecutor)_driver)
                .ExecuteScript(
                    @"
                    const field =
                        document.getElementById('rest-name');

                    if (!field)
                        return '';

                    const form =
                        field.closest('form');

                    if (!form)
                        return '';

                    const fields =
                        [...form.querySelectorAll(
                            'input, select, textarea'
                        )];

                    const invalid =
                        fields.filter(
                            x => !x.checkValidity()
                        );

                    return invalid
                        .map(x => {
                            const id =
                                x.id ||
                                x.name ||
                                x.type ||
                                x.tagName;

                            const message =
                                x.validationMessage ||
                                'invalid';

                            return id + ': ' + message;
                        })
                        .join(' | ');
                    ");

            return result?.ToString()
                   ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    // ============================================================
    // ERROR / SUCCESS
    // FIXED SECTION
    // ============================================================

    private IWebElement? FindVisibleError()
    {
        var elements =
            _driver.FindElements(
                By.CssSelector(
                    ".pd-form-error," +
                    ".Toastify__toast--error," +
                    ".toast-error," +
                    ".alert-danger," +
                    ".error-message"));

        return elements.FirstOrDefault(element =>
        {
            if (!SafeDisplayed(
                    element))
            {
                return false;
            }

            var text =
                element.Text
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    text))
            {
                return false;
            }

            // Never classify a success notification as an error.
            if (text.Contains(
                    "success",
                    StringComparison.OrdinalIgnoreCase)
                ||
                text.Contains(
                    "listing created",
                    StringComparison.OrdinalIgnoreCase)
                ||
                text.Contains(
                    "listing updated",
                    StringComparison.OrdinalIgnoreCase)
                ||
                text.Contains(
                    "created successfully",
                    StringComparison.OrdinalIgnoreCase)
                ||
                text.Contains(
                    "updated successfully",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        });
    }

    private bool HasVisibleSuccessMessage()
    {
        var elements =
            _driver.FindElements(
                By.XPath(
                    "//*[" +
                    "contains(" +
                    "translate(normalize-space(.)," +
                    "'ABCDEFGHIJKLMNOPQRSTUVWXYZ'," +
                    "'abcdefghijklmnopqrstuvwxyz')," +
                    "'listing created')" +
                    " or contains(" +
                    "translate(normalize-space(.)," +
                    "'ABCDEFGHIJKLMNOPQRSTUVWXYZ'," +
                    "'abcdefghijklmnopqrstuvwxyz')," +
                    "'listing updated')" +
                    " or contains(" +
                    "translate(normalize-space(.)," +
                    "'ABCDEFGHIJKLMNOPQRSTUVWXYZ'," +
                    "'abcdefghijklmnopqrstuvwxyz')," +
                    "'success')" +
                    "]"));

        return elements.Any(
            SafeDisplayed);
    }

    // ============================================================
    // HELPERS
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

    private bool IsRestaurantFormVisible()
    {
        return _driver
            .FindElements(
                By.Id("rest-name"))
            .Any(
                SafeDisplayed);
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

    private static void ClearInput(
        IWebElement element)
    {
        element.Click();

        element.SendKeys(
            Keys.Control + "a");

        element.SendKeys(
            Keys.Backspace);
    }

    private static void SelectOption(
        IWebElement element,
        string expected)
    {
        var select =
            new SelectElement(
                element);

        var option =
            select.Options.FirstOrDefault(o =>
                string.Equals(
                    o.Text.Trim(),
                    expected,
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    o.GetAttribute("value"),
                    expected,
                    StringComparison.OrdinalIgnoreCase)
                ||
                o.Text.Contains(
                    expected,
                    StringComparison.OrdinalIgnoreCase));

        if (option == null)
        {
            throw new NoSuchElementException(
                $"Option '{expected}' was not found.");
        }

        option.Click();
    }

    private void SetTime(
        IWebElement element,
        string value)
    {
        ScrollIntoView(
            element);

        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(
                @"
                const input = arguments[0];
                const value = arguments[1];

                const descriptor =
                    Object.getOwnPropertyDescriptor(
                        HTMLInputElement.prototype,
                        'value'
                    );

                if (descriptor && descriptor.set)
                    descriptor.set.call(input, value);
                else
                    input.value = value;

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