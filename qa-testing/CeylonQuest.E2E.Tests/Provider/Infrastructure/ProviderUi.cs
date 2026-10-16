using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CeylonQuest.E2E.Tests.Infrastructure;
using OpenQA.Selenium;
using Xunit;

namespace CeylonQuest.E2E.Tests.Provider.Infrastructure;

internal sealed record UiResponse(int Sequence, string Url, int Status, JsonElement Body);

internal sealed class ProviderUi(BrowserSession browser)
{
    public BrowserSession Browser { get; } = browser;
    public static string Normalize(string text) => Regex.Replace(text, @"\s+", " ").Trim();
    public static string SriLankaTime(string timestamp)
    {
        var utc = DateTimeOffset.Parse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        return TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo"))
            .ToString("MMM d, yyyy | h:mm tt", CultureInfo.GetCultureInfo("en-US"));
    }
    public static void EqualText(string expected, string actual, bool ignoreCase = false) =>
        Assert.Equal(Normalize(expected), Normalize(actual), ignoreCase: ignoreCase);
    public static string Literal(string text) => !text.Contains('\'') ? "'" + text + "'" :
        !text.Contains('"') ? "\"" + text + "\"" : "concat('" + text.Replace("'", "',\"'\",'") + "')";

    public void Observe()
    {
        Browser.Script.ExecuteScript("""
            if (!window.__cqProviderReads) {
              const state = window.__cqProviderReads = {sequence: 0, responses: [], writes: []};
              const original = window.fetch;
              window.fetch = async function(...args) {
                const method = String(args[1]?.method || args[0]?.method || 'GET').toUpperCase();
                const sequence = ++state.sequence;
                const requestUrl = new URL(args[0] instanceof Request ? args[0].url : args[0], location.href);
                if (!['GET', 'HEAD', 'OPTIONS'].includes(method) &&
                    (/^\/api\/catalog\/(accommodation-listings|restaurant-listings|activity-listings)(\/|$)/.test(requestUrl.pathname) ||
                     requestUrl.pathname.startsWith('/api/provider-bookings/') ||
                     requestUrl.pathname.startsWith('/api/bookings/reviews/') ||
                     requestUrl.pathname.startsWith('/api/notifications'))) {
                  state.writes.push({sequence, method, path: requestUrl.pathname});
                }
                const response = await original.apply(this, args);
                const path = new URL(response.url).pathname;
                const watch = /^\/api\/catalog\/(provider\/profile|accommodation-listings|restaurant-listings|activity-listings)$/.test(path) ||
                  path === '/api/provider-bookings/my' || path === '/api/notifications' ||
                  /^\/api\/bookings\/reviews\/[0-9a-f-]+$/i.test(path);
                if (method === 'GET' && watch) {
                  response.clone().json().then(body => {
                    state.responses.push({sequence, url: response.url, status: response.status, body});
                    if (state.responses.length > 200) state.responses.shift();
                  }).catch(() => {
                    state.responses.push({sequence, url: response.url, status: response.status, body: null});
                  });
                }
                return response;
              };
            }
            """);
    }

    public int Sequence => Convert.ToInt32(Browser.Script.ExecuteScript("return window.__cqProviderReads?.sequence || 0"));
    public void AssertNoBusinessWritesSince(int sequence) => Assert.Equal(0, Convert.ToInt32(Browser.Script.ExecuteScript(
        "return (window.__cqProviderReads?.writes || []).filter(item => item.sequence > arguments[0]).length", sequence)));
    public UiResponse? Latest(string path, int after = -1, Func<Uri, bool>? query = null)
    {
        var json = Browser.Script.ExecuteScript("return JSON.stringify(window.__cqProviderReads?.responses || [])") as string
            ?? throw new InvalidOperationException("Provider response observation was lost after document navigation.");
        using var document = JsonDocument.Parse(json);
        var record = document.RootElement.EnumerateArray().Where(item => item.GetProperty("sequence").GetInt32() > after &&
            new Uri(item.GetProperty("url").GetString()!).AbsolutePath.Equals(path, StringComparison.OrdinalIgnoreCase) &&
            (query is null || query(new Uri(item.GetProperty("url").GetString()!))))
            .OrderByDescending(item => item.GetProperty("sequence").GetInt32()).FirstOrDefault();
        return record.ValueKind == JsonValueKind.Undefined ? null : new UiResponse(record.GetProperty("sequence").GetInt32(),
            record.Text("url"), record.GetProperty("status").GetInt32(), record.GetProperty("body").Clone());
    }

    public UiResponse Wait(string path, int after = -1, Func<Uri, bool>? query = null)
    {
        return Browser.Until(_ => {
            var response = Latest(path, after, query);
            if (response is null) return null!;
            Assert.True(response.Status is >= 200 and < 300, $"Actual UI request {path} returned HTTP {response.Status}.");
            Assert.NotEqual(JsonValueKind.Null, response.Body.ValueKind);
            return response;
        }, "No successful UI response for " + path + ". Check the service, role and deployed frontend contract.");
    }

    public static string Query(Uri url, string key)
    {
        return url.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2)).Where(part => Uri.UnescapeDataString(part[0]) == key)
            .Select(part => part.Length > 1 ? Uri.UnescapeDataString(part[1].Replace('+', ' ')) : "").FirstOrDefault() ?? "";
    }

    public void ClickText(string scope, string text) => Browser.Click(By.XPath(
        $"{scope}//button[normalize-space()={Literal(text)}]"));
    public void AssertNoVisibleError(string scope)
    {
        var errors = Browser.Driver.FindElements(By.CssSelector(scope + " [role='alert']"))
            .Where(element => element.Displayed).Select(element => element.Text).ToArray();
        Assert.True(errors.Length == 0, "UI reported an error: " + string.Join("; ", errors));
    }

    public static string BrowserMoney(BrowserSession browser, decimal amount, int decimals) =>
        browser.Script.ExecuteScript("""
            return new Intl.NumberFormat('en-LK', {style:'currency', currency:'LKR',
              minimumFractionDigits:arguments[1], maximumFractionDigits:arguments[1]}).format(arguments[0]);
            """, (double)amount, decimals) as string ?? throw new InvalidOperationException("Currency formatting failed.");
}

internal static class ProviderJson
{
    public static string Text(this JsonElement value, string property, string fallback = "") =>
        value.TryGetProperty(property, out var item) && item.ValueKind != JsonValueKind.Null ? item.GetString() ?? fallback : fallback;
    public static int Int(this JsonElement value, string property) => value.GetProperty(property).GetInt32();
    public static decimal Money(this JsonElement value, string property) => value.GetProperty(property).GetDecimal();
    public static bool Flag(this JsonElement value, string property) => value.GetProperty(property).GetBoolean();
    public static JsonElement[] Items(this JsonElement value) => value.GetProperty("items").EnumerateArray().ToArray();
    public static string ListingName(this JsonElement value) => value.Text("title", value.Text("name", value.Text("roomType")));
    public static DateOnly Date(this JsonElement value, string property) =>
        DateOnly.ParseExact(value.Text(property)[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
