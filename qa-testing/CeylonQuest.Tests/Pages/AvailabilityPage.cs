using System.Text.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages;

public class AvailabilityPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public sealed record ApiResult(int Status, string Body);

    public AvailabilityPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
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

            const headers = { 'Accept': 'application/json' };
            if (body) headers['Content-Type'] = 'application/json';

            if (useAuth) {
                const token = window.localStorage.getItem('authToken');
                if (token) headers['Authorization'] = 'Bearer ' + token;
            }

            fetch(path, {
                method,
                headers,
                body: body || undefined
            })
            .then(async response => {
                const text = await response.text();
                done(JSON.stringify({ status: response.status, body: text }));
            })
            .catch(error => done(JSON.stringify({ status: 0, body: String(error) })));
        ";

        var raw = (string?)((IJavaScriptExecutor)_driver)
            .ExecuteAsyncScript(script, method, path, bodyJson, useAuth);

        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Browser API request returned no result.");

        using var doc = JsonDocument.Parse(raw);
        return new ApiResult(
            doc.RootElement.GetProperty("status").GetInt32(),
            doc.RootElement.GetProperty("body").GetString() ?? string.Empty);
    }

    public JsonDocument GetAvailability(string listingId, string date)
    {
        var result = BrowserApiRequest(
            "GET",
            $"/api/catalog/availability/{listingId}?date={date}",
            null,
            false);

        if (result.Status != 200)
            throw new InvalidOperationException($"Availability GET failed. HTTP {result.Status}: {result.Body}");

        return JsonDocument.Parse(result.Body);
    }

    public ApiResult SetCapacity(string listingId, string date, string timeSlot, int capacity)
    {
        var body = JsonSerializer.Serialize(new
        {
            date,
            timeSlot,
            capacity
        });

        return BrowserApiRequest(
            "PUT",
            $"/api/catalog/availability/{listingId}",
            body,
            true);
    }

    public ApiResult SetRawCapacityPayload(string listingId, string rawJson)
    {
        return BrowserApiRequest(
            "PUT",
            $"/api/catalog/availability/{listingId}",
            rawJson,
            true);
    }

    public ApiResult SimulateBooking(
        string listingId,
        string date,
        string timeSlot,
        int guestCount)
    {
        // NOTE: Current dev DTO uses Date + GuestCount.
        var body = JsonSerializer.Serialize(new
        {
            listingId,
            date,
            timeSlot,
            guestCount
        });

        return BrowserApiRequest(
            "POST",
            "/api/catalog/availability/simulate-booking-event",
            body,
            false);
    }

    public (string TimeSlot, int Total, int Remaining, bool FullyBooked) FirstSlot(JsonDocument doc)
    {
        var slots = doc.RootElement.GetProperty("slots");
        if (slots.GetArrayLength() == 0)
            throw new InvalidOperationException("The selected listing/date returned no availability slots.");

        var slot = slots[0];
        return (
            slot.GetProperty("timeSlot").GetString() ?? string.Empty,
            slot.GetProperty("totalCapacity").GetInt32(),
            slot.GetProperty("remainingCapacity").GetInt32(),
            slot.GetProperty("isFullyBooked").GetBoolean());
    }

    public void OpenVisitorExplore()
    {
        var explore = _wait.Until(d =>
            d.FindElements(By.Id("nav-explore"))
             .FirstOrDefault(e => e.Displayed && e.Enabled));

        if (explore == null)
            throw new NoSuchElementException("Visitor Explore navigation was not found.");

        explore.Click();
        _wait.Until(d => d.FindElements(By.CssSelector(".vd-service-card")).Any(e => e.Displayed));
    }

    public void OpenAvailabilityForListingTitle(string listingTitle)
    {
        var card = _wait.Until(d =>
            d.FindElements(By.CssSelector(".vd-service-card"))
             .FirstOrDefault(c =>
             {
                 try
                 {
                     var title = c.FindElement(By.CssSelector(".vd-service-card__title")).Text.Trim();
                     return title.Equals(listingTitle, StringComparison.OrdinalIgnoreCase);
                 }
                 catch
                 {
                     return false;
                 }
             }));

        if (card == null)
            throw new NoSuchElementException($"Listing '{listingTitle}' was not visible in Explore.");

        card.FindElement(By.CssSelector(".vd-book-btn")).Click();

        _wait.Until(d =>
            d.FindElements(By.CssSelector(".vd-detail-modal__title"))
             .Any(e => e.Displayed && e.Text.Contains("Book:", StringComparison.OrdinalIgnoreCase)));
    }

    public void SelectDate(string yyyyMmDd)
    {
        var date = _wait.Until(d => d.FindElement(By.CssSelector("input[type='date']")));
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            const el = arguments[0];
            const value = arguments[1];
            const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set;
            setter.call(el, value);
            el.dispatchEvent(new Event('input', { bubbles: true }));
            el.dispatchEvent(new Event('change', { bubbles: true }));
        ", date, yyyyMmDd);

        Thread.Sleep(700);
    }

    public string AvailabilityMessage()
    {
        var box = _wait.Until(d =>
            d.FindElements(By.XPath("//div[contains(.,'spots available') or contains(.,'Fully Booked') or contains(.,'does not operate')]"))
             .FirstOrDefault(e => e.Displayed));

        return box?.Text ?? string.Empty;
    }

    public bool ConfirmSelectionEnabled()
    {
        var button = _wait.Until(d =>
            d.FindElements(By.CssSelector(".vd-save-btn"))
             .LastOrDefault(e => e.Displayed));

        return button != null && button.Enabled &&
               !button.Text.Contains("Fully Booked", StringComparison.OrdinalIgnoreCase);
    }
}
