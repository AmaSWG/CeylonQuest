using System.Text.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages;

public class InventoryReportPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public sealed record ApiResult(int Status, string Body);

    public InventoryReportPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(
            driver,
            TimeSpan.FromSeconds(30));
    }

    private IWebElement? FirstVisible(params By[] selectors)
    {
        foreach (var selector in selectors)
        {
            try
            {
                var element =
                    _driver.FindElements(selector)
                        .FirstOrDefault(e => e.Displayed);

                if (element != null)
                    return element;
            }
            catch (StaleElementReferenceException)
            {
            }
        }

        return null;
    }

    private void SafeClick(IWebElement element)
    {
        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});",
                element);

        try
        {
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            ((IJavaScriptExecutor)_driver)
                .ExecuteScript(
                    "arguments[0].click();",
                    element);
        }
    }

    private void WaitUntilLoaded()
    {
        // Give React time to enter loading state.
        Thread.Sleep(150);

        _wait.Until(d =>
        {
            try
            {
                var loading =
                    d.FindElements(
                            By.CssSelector(".pd-loading"))
                        .Any(e => e.Displayed);

                if (loading)
                    return false;

                // Report finished successfully.
                var hasKpis =
                    d.FindElements(
                            By.CssSelector(
                                ".cq-report-kpigrid"))
                        .Any(e => e.Displayed);

                // Or report finished with an error.
                var hasError =
                    d.FindElements(
                            By.CssSelector(
                                ".cq-report-alert--danger"))
                        .Any(e => e.Displayed);

                return hasKpis || hasError;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });
    }

    public ApiResult BrowserApiRequest(
        string method,
        string path,
        string? bodyJson = null,
        bool useAuth = true)
    {
        var script = @"
            const done = arguments[arguments.length - 1];
            const method = arguments[0];
            const path = arguments[1];
            const body = arguments[2];
            const useAuth = arguments[3];

            const headers = {
                'Accept': 'application/json'
            };

            if (body)
                headers['Content-Type'] =
                    'application/json';

            if (useAuth) {
                const token =
                    window.localStorage.getItem(
                        'authToken');

                if (token)
                    headers['Authorization'] =
                        'Bearer ' + token;
            }

            fetch(path, {
                method,
                headers,
                body: body || undefined
            })
            .then(async response => {
                const text =
                    await response.text();

                done(JSON.stringify({
                    status: response.status,
                    body: text
                }));
            })
            .catch(error =>
                done(JSON.stringify({
                    status: 0,
                    body: String(error)
                })));
        ";

        var raw =
            (string?)((IJavaScriptExecutor)_driver)
                .ExecuteAsyncScript(
                    script,
                    method,
                    path,
                    bodyJson,
                    useAuth);

        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "Browser API request returned no result.");
        }

        using var doc =
            JsonDocument.Parse(raw);

        return new ApiResult(
            doc.RootElement
                .GetProperty("status")
                .GetInt32(),

            doc.RootElement
                .GetProperty("body")
                .GetString()
                ?? string.Empty);
    }

    public ApiResult GetInventoryReportApi(
        string? startDate = null,
        string? endDate = null,
        string? category = null,
        string? location = null)
    {
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(startDate))
        {
            query.Add(
                $"startDate={Uri.EscapeDataString(startDate)}");
        }

        if (!string.IsNullOrWhiteSpace(endDate))
        {
            query.Add(
                $"endDate={Uri.EscapeDataString(endDate)}");
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query.Add(
                $"category={Uri.EscapeDataString(category)}");
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            query.Add(
                $"location={Uri.EscapeDataString(location)}");
        }

        var suffix =
            query.Count == 0
                ? string.Empty
                : "?" + string.Join("&", query);

        return BrowserApiRequest(
            "GET",
            "/api/catalog/reports/inventory-summary"
            + suffix,
            null,
            true);
    }

    // =========================
    // PROVIDER REPORT
    // =========================

    public void OpenProviderReport()
    {
        var nav =
            _wait.Until(d =>
            {
                try
                {
                    return d.FindElements(
                            By.Id("pd-nav-reports"))
                        .FirstOrDefault(e =>
                            e.Displayed
                            && e.Enabled);
                }
                catch (
                    StaleElementReferenceException)
                {
                    return null;
                }
            });

        nav ??= FirstVisible(
            By.XPath(
                "//button[contains(normalize-space(.),'Report')]"),

            By.XPath(
                "//a[contains(normalize-space(.),'Report')]"));

        if (nav == null)
        {
            throw new NoSuchElementException(
                "Provider report navigation was not found.");
        }

        SafeClick(nav);

        WaitUntilLoaded();
    }

    // =========================
    // ADMIN REPORT
    // =========================

    public void OpenAdminReport()
    {
        var nav =
            _wait.Until(d =>
            {
                try
                {
                    return d.FindElements(
                            By.Id("ad-nav-reports"))
                        .FirstOrDefault(e =>
                            e.Displayed
                            && e.Enabled);
                }
                catch (
                    StaleElementReferenceException)
                {
                    return null;
                }
            });

        nav ??= FirstVisible(
            By.XPath(
                "//button[contains(normalize-space(.),'Report')]"),

            By.XPath(
                "//a[contains(normalize-space(.),'Report')]"));

        if (nav == null)
        {
            throw new NoSuchElementException(
                "Admin report navigation was not found.");
        }

        SafeClick(nav);

        WaitUntilLoaded();
    }

    // =========================
    // HEADINGS
    // =========================

    public bool HasProviderHeading()
    {
        return _driver
            .FindElements(
                By.CssSelector(".cq-inv-report h1"))
            .Any(e =>
                e.Displayed
                &&
                e.Text.Contains(
                    "Live Availability & Capacity Report",
                    StringComparison.OrdinalIgnoreCase));
    }

    public bool HasAdminHeading()
    {
        return _driver
            .FindElements(
                By.CssSelector(".cq-inv-report h1"))
            .Any(e =>
                e.Displayed
                &&
                e.Text.Contains(
                    "Island-wide Listings & Inventory Report",
                    StringComparison.OrdinalIgnoreCase));
    }

    // =========================
    // REPORT SECTIONS
    // =========================

    public bool HasCategorySection()
    {
        return _driver
            .FindElements(
                By.CssSelector(
                    ".cq-report-box__title"))
            .Any(e =>
                e.Displayed
                &&
                e.Text.Contains(
                    "Category Distribution & Inventory",
                    StringComparison.OrdinalIgnoreCase));
    }

    public bool HasLocationSection()
    {
        return _driver
            .FindElements(
                By.CssSelector(
                    ".cq-report-box__title"))
            .Any(e =>
                e.Displayed
                &&
                e.Text.Contains(
                    "Regional Supply & Coverage Matrix",
                    StringComparison.OrdinalIgnoreCase));
    }

    public bool HasLowAvailabilitySection()
    {
        return _driver
            .FindElements(
                By.CssSelector(
                    ".cq-report-section__header h2"))
            .Any(e =>
                e.Displayed
                &&
                e.Text.Contains(
                    "Capacity & Low-Availability Alerts",
                    StringComparison.OrdinalIgnoreCase));
    }

    // =========================
    // CATEGORY / LOCATION ROWS
    // =========================

    public int CategoryDataRowCount()
    {
        var box =
            FindBoxByTitle(
                "Category Distribution & Inventory");

        if (box == null)
            return 0;

        return box
            .FindElements(
                By.CssSelector("tbody tr"))
            .Count(e => e.Displayed);
    }

    public int LocationDataRowCount()
    {
        var box =
            FindBoxByTitle(
                "Regional Supply & Coverage Matrix");

        if (box == null)
            return 0;

        return box
            .FindElements(
                By.CssSelector("tbody tr"))
            .Count(e => e.Displayed);
    }

    private IWebElement? FindBoxByTitle(
        string title)
    {
        var heading =
            _driver
                .FindElements(
                    By.XPath(
                        $"//h3[contains(normalize-space(.), \"{title}\")]"))
                .FirstOrDefault(
                    e => e.Displayed);

        if (heading == null)
            return null;

        return heading
            .FindElements(
                By.XPath(
                    "./ancestor::div[contains(@class,'cq-report-box')][1]"))
            .FirstOrDefault();
    }

    // =========================
    // SOLD OUT / LOW STOCK
    // =========================

    public bool HasSoldOutAlert()
    {
        return _driver
            .FindElements(
                By.XPath(
                    "//*[contains(@class,'cq-statusbadge') and contains(normalize-space(.),'Sold Out')]"))
            .Any(e => e.Displayed);
    }

    public bool HasLowStockAlert()
    {
        return _driver
            .FindElements(
                By.XPath(
                    "//*[contains(@class,'cq-statusbadge') and contains(normalize-space(.),'Low Stock')]"))
            .Any(e => e.Displayed);
    }

    // =========================
    // DATE PRESETS
    // =========================

    public void SelectPreset(
        string label)
    {
        var button =
            _wait.Until(d =>
            {
                try
                {
                    return d
                        .FindElements(
                            By.CssSelector(
                                ".cq-preset-btn"))
                        .FirstOrDefault(e =>
                            e.Displayed
                            &&
                            e.Enabled
                            &&
                            e.Text.Trim()
                                .Equals(
                                    label,
                                    StringComparison
                                        .OrdinalIgnoreCase));
                }
                catch (
                    StaleElementReferenceException)
                {
                    return null;
                }
            });

        if (button == null)
        {
            throw new NoSuchElementException(
                $"Preset '{label}' was not found.");
        }

        SafeClick(button);

        _wait.Until(d =>
            d.FindElements(
                    By.CssSelector(
                        ".cq-preset-btn.active"))
                .Any(e =>
                    e.Displayed
                    &&
                    e.Text.Trim()
                        .Equals(
                            label,
                            StringComparison
                                .OrdinalIgnoreCase)));

        WaitUntilLoaded();
    }

    public bool IsPresetActive(
        string label)
    {
        return _driver
            .FindElements(
                By.CssSelector(
                    ".cq-preset-btn.active"))
            .Any(e =>
                e.Displayed
                &&
                e.Text.Trim()
                    .Equals(
                        label,
                        StringComparison
                            .OrdinalIgnoreCase));
    }

    // =========================
    // CUSTOM DATE RANGE
    // =========================

    public void SetCustomDateRange(
        string startDate,
        string endDate)
    {
        SelectPresetWithoutLoading(
            "Custom Range");

        SetDateInput(
            "inv-start-date",
            startDate);

        SetDateInput(
            "inv-end-date",
            endDate);

        ClickApply();
    }

    private void SelectPresetWithoutLoading(
        string label)
    {
        var button =
            _wait.Until(d =>
                d.FindElements(
                        By.CssSelector(
                            ".cq-preset-btn"))
                    .FirstOrDefault(e =>
                        e.Displayed
                        &&
                        e.Enabled
                        &&
                        e.Text.Trim()
                            .Equals(
                                label,
                                StringComparison
                                    .OrdinalIgnoreCase)));

        if (button == null)
        {
            throw new NoSuchElementException(
                $"Preset '{label}' was not found.");
        }

        SafeClick(button);

        _wait.Until(d =>
            d.FindElements(
                    By.CssSelector(
                        ".cq-preset-btn.active"))
                .Any(e =>
                    e.Displayed
                    &&
                    e.Text.Trim()
                        .Equals(
                            label,
                            StringComparison
                                .OrdinalIgnoreCase)));
    }

    private void SetDateInput(
        string id,
        string yyyyMmDd)
    {
        var input =
            _wait.Until(d =>
                d.FindElements(
                        By.Id(id))
                    .FirstOrDefault(e =>
                        e.Displayed
                        &&
                        e.Enabled));

        if (input == null)
        {
            throw new NoSuchElementException(
                $"Date input '{id}' was not found.");
        }

        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(@"
                const el = arguments[0];
                const value = arguments[1];

                const setter =
                    Object.getOwnPropertyDescriptor(
                        HTMLInputElement.prototype,
                        'value').set;

                setter.call(el, value);

                el.dispatchEvent(
                    new Event(
                        'input',
                        { bubbles: true }));

                el.dispatchEvent(
                    new Event(
                        'change',
                        { bubbles: true }));
            ",
            input,
            yyyyMmDd);
    }

    public string StartDateValue()
    {
        var input =
            _driver
                .FindElements(
                    By.Id("inv-start-date"))
                .FirstOrDefault(
                    e => e.Displayed);

        return input?
                   .GetAttribute("value")
               ?? string.Empty;
    }

    public string EndDateValue()
    {
        var input =
            _driver
                .FindElements(
                    By.Id("inv-end-date"))
                .FirstOrDefault(
                    e => e.Displayed);

        return input?
                   .GetAttribute("value")
               ?? string.Empty;
    }

    // =========================
    // CATEGORY FILTER
    // =========================

    public void ApplyCategory(
        string value)
    {
        var element =
            _wait.Until(d =>
                d.FindElements(
                        By.Id("inv-category"))
                    .FirstOrDefault(e =>
                        e.Displayed
                        &&
                        e.Enabled));

        if (element == null)
        {
            throw new NoSuchElementException(
                "Category filter was not found.");
        }

        var select =
            new SelectElement(element);

        var option =
            select.Options
                .FirstOrDefault(o =>
                    string.Equals(
                        o.GetAttribute("value"),
                        value,
                        StringComparison
                            .OrdinalIgnoreCase)
                    ||
                    o.Text.Trim()
                        .Equals(
                            value,
                            StringComparison
                                .OrdinalIgnoreCase)
                    ||
                    o.Text.Contains(
                        value,
                        StringComparison
                            .OrdinalIgnoreCase));

        if (option == null)
        {
            throw new NoSuchElementException(
                $"Category option '{value}' was not found.");
        }

        option.Click();

        ClickApply();
    }

    public string SelectedCategory()
    {
        var element =
            _wait.Until(d =>
                d.FindElements(
                        By.Id("inv-category"))
                    .FirstOrDefault(
                        e => e.Displayed));

        if (element == null)
        {
            throw new NoSuchElementException(
                "Category filter was not found.");
        }

        return new SelectElement(element)
                   .SelectedOption
                   .GetAttribute("value")
               ?? string.Empty;
    }

    // =========================
    // LOCATION FILTER
    // =========================

    public void ApplyLocation(
        string location)
    {
        var input =
            _wait.Until(d =>
                d.FindElements(
                        By.Id("inv-location"))
                    .FirstOrDefault(e =>
                        e.Displayed
                        &&
                        e.Enabled));

        if (input == null)
        {
            throw new NoSuchElementException(
                "Location filter was not found.");
        }

        input.Clear();
        input.SendKeys(location);

        ClickApply();
    }

    public string LocationValue()
    {
        var input =
            _wait.Until(d =>
                d.FindElements(
                        By.Id("inv-location"))
                    .FirstOrDefault(
                        e => e.Displayed));

        if (input == null)
        {
            throw new NoSuchElementException(
                "Location filter was not found.");
        }

        return input.GetAttribute("value")
               ?? string.Empty;
    }

    // =========================
    // COMBINED FILTER
    // =========================

    public void ApplyCombined(
        string category,
        string location)
    {
        var categoryElement =
            _wait.Until(d =>
                d.FindElements(
                        By.Id("inv-category"))
                    .FirstOrDefault(e =>
                        e.Displayed
                        &&
                        e.Enabled));

        if (categoryElement == null)
        {
            throw new NoSuchElementException(
                "Category filter was not found.");
        }

        var select =
            new SelectElement(
                categoryElement);

        var option =
            select.Options
                .FirstOrDefault(o =>
                    string.Equals(
                        o.GetAttribute("value"),
                        category,
                        StringComparison
                            .OrdinalIgnoreCase)
                    ||
                    o.Text.Trim()
                        .Equals(
                            category,
                            StringComparison
                                .OrdinalIgnoreCase)
                    ||
                    o.Text.Contains(
                        category,
                        StringComparison
                            .OrdinalIgnoreCase));

        if (option == null)
        {
            throw new NoSuchElementException(
                $"Category option '{category}' was not found.");
        }

        option.Click();

        var input =
            _wait.Until(d =>
                d.FindElements(
                        By.Id("inv-location"))
                    .FirstOrDefault(e =>
                        e.Displayed
                        &&
                        e.Enabled));

        if (input == null)
        {
            throw new NoSuchElementException(
                "Location filter was not found.");
        }

        input.Clear();
        input.SendKeys(location);

        ClickApply();
    }

    // =========================
    // APPLY
    // =========================

    public void ClickApply()
    {
        var button =
            _wait.Until(d =>
                d.FindElements(
                        By.CssSelector(
                            ".cq-report-btn--primary"))
                    .FirstOrDefault(e =>
                        e.Displayed
                        &&
                        e.Enabled));

        if (button == null)
        {
            throw new NoSuchElementException(
                "Apply Filters button was not found.");
        }

        SafeClick(button);

        WaitUntilLoaded();
    }

    // =========================
    // NO DATA
    // =========================

    public bool ShowsNoDataState()
    {
        return _driver
            .FindElements(
                By.CssSelector(
                    ".cq-report-empty"))
            .Any(e => e.Displayed);
    }

    // =========================
    // KPI
    // =========================

    public string KpiValue(
        string label)
    {
        var card =
            _driver
                .FindElements(
                    By.CssSelector(
                        ".cq-kpicard"))
                .FirstOrDefault(c =>
                {
                    try
                    {
                        var labelElement =
                            c.FindElements(
                                    By.CssSelector(
                                        ".cq-kpicard__label"))
                                .FirstOrDefault();

                        return c.Displayed
                               &&
                               labelElement != null
                               &&
                               labelElement.Text
                                   .Trim()
                                   .Equals(
                                       label,
                                       StringComparison
                                           .OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                });

        if (card == null)
        {
            throw new NoSuchElementException(
                $"KPI card '{label}' was not found.");
        }

        var value =
            card.FindElements(
                    By.CssSelector(
                        ".cq-kpicard__val"))
                .FirstOrDefault(
                    e => e.Displayed);

        if (value == null)
        {
            throw new NoSuchElementException(
                $"KPI value '{label}' was not found.");
        }

        return value.Text.Trim();
    }

    // =========================
    // RESET
    // =========================

    public void Reset()
    {
        var button =
            _wait.Until(d =>
                d.FindElements(
                        By.CssSelector(
                            ".cq-report-btn--secondary"))
                    .FirstOrDefault(e =>
                        e.Displayed
                        &&
                        e.Enabled));

        if (button == null)
        {
            throw new NoSuchElementException(
                "Reset button was not found.");
        }

        SafeClick(button);

        WaitUntilLoaded();
    }
}