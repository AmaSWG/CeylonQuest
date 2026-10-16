using System.Text.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Infrastructure;

internal sealed class BrowserSession : IDisposable
{
    private readonly ITestOutputHelper output;
    private readonly string testName;
    private readonly string evidenceDirectory;
    public IWebDriver Driver { get; }
    public IJavaScriptExecutor Script => (IJavaScriptExecutor)Driver;

    public BrowserSession(string testName, ITestOutputHelper output)
    {
        this.testName = testName;
        this.output = output;
        evidenceDirectory = Path.GetFullPath(TestSettings.Optional("CQ_E2E_EVIDENCE_DIR") ??
            Path.Combine(AppContext.BaseDirectory, "TestResults", "screenshots"));
        var options = new ChromeOptions();
        if (TestSettings.Headless) options.AddArgument("--headless=new");
        options.AddArgument("--window-size=1440,1100");
        options.AddArgument("--lang=en-US");
        // No driver executable/package is pinned: Selenium Manager resolves ChromeDriver.
        var service = ChromeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;
        try
        {
            Driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(90));
        }
        catch { service.Dispose(); throw; }
        try
        {
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
            Driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(15);
        }
        catch { Driver.Dispose(); throw; }
    }

    public T Until<T>(Func<IWebDriver, T> condition, string message, int? seconds = null)
    {
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds ?? TestSettings.Seconds("CQ_E2E_WAIT_SECONDS", 35)))
        { PollingInterval = TimeSpan.FromMilliseconds(250), Message = message };
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
        return wait.Until(condition);
    }

    public IWebElement Visible(By selector) => Until(d =>
        d.FindElements(selector).FirstOrDefault(e => e.Displayed)!, $"Visible element required: {selector}");

    public void Click(By selector) => Until(d =>
    {
        var element = d.FindElements(selector).FirstOrDefault(e => e.Displayed && e.Enabled);
        if (element is null) return false;
        try { element.Click(); return true; }
        catch (ElementClickInterceptedException) { return false; }
    }, $"Clickable element required: {selector}");

    public void Fill(By selector, string value)
    {
        var element = Visible(selector);
        element.SendKeys(Keys.Control + "a");
        element.SendKeys(Keys.Backspace);
        element.SendKeys(value);
    }

    public void SetDate(By selector, DateOnly date)
    {
        // Native HTML date controls have locale-dependent keyboard input. Dispatch the
        // same input/change events React observes, without changing min/max or validation.
        SetDateValue(selector, date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
    }

    public void ClearDate(By selector) => SetDateValue(selector, "");

    private void SetDateValue(By selector, string value)
    {
        Script.ExecuteScript("""
            const input = arguments[0];
            Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(input, arguments[1]);
            input.dispatchEvent(new Event('input', {bubbles: true}));
            input.dispatchEvent(new Event('change', {bubbles: true}));
            """, Visible(selector), value);
        Until(_ => Visible(selector).GetDomProperty("value") == value, "Date input did not retain the requested date.");
    }

    public void Step(string name) => output.WriteLine($"{DateTimeOffset.UtcNow:O} {name}");

    public void Run(Action body)
    {
        try { body(); }
        catch { Capture("failure"); throw; }
    }

    public void Capture(string stage)
    {
        try
        {
            Directory.CreateDirectory(evidenceDirectory);
            var path = Path.Combine(evidenceDirectory, $"{testName}-{stage}-{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}.png");
            ((ITakesScreenshot)Driver).GetScreenshot().SaveAsFile(path);
            output.WriteLine("Screenshot: " + path);
        }
        catch (Exception error) { output.WriteLine("Screenshot unavailable: " + error.GetType().Name); }
    }

    // Passive observation of the real UI's successful GET responses. No requests,
    // tokens, auth state, booking state or server responses are changed. Full IDs are
    // absent from the rendered booking table/notification cards, so they are used
    // solely to correlate visible evidence to this test's newly created booking.
    public void ObserveUiReads()
    {
        Script.ExecuteScript("""
            if (!window.__cqE2eReads) {
              window.__cqE2eReads = {};
              const original = window.fetch;
              window.fetch = async function(...args) {
                const response = await original.apply(this, args);
                const url = new URL(response.url);
                if (response.ok && (!args[1]?.method || args[1].method === 'GET') &&
                    (url.pathname === '/api/user-bookings/my' || url.pathname === '/api/notifications')) {
                  response.clone().json().then(body => {
                    window.__cqE2eReads[url.pathname] = {url: response.url, body};
                  }).catch(() => {});
                }
                return response;
              };
            }
            """);
    }

    public JsonElement? ReadUiPayload(string path)
    {
        var json = Script.ExecuteScript("return JSON.stringify(window.__cqE2eReads?.[arguments[0]]?.body) || null", path) as string;
        return json is null ? null : JsonSerializer.Deserialize<JsonElement>(json);
    }

    public void Dispose() => Driver.Dispose();
}
