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
    public bool ObservationInstalled { get; private set; }
    public static string Normalize(string text) => Regex.Replace(text, @"\s+", " ").Trim();
    public static void EqualText(string expected, string actual, bool ignoreCase = false) =>
        Assert.Equal(Normalize(expected), Normalize(actual), ignoreCase: ignoreCase);
    public static string Literal(string text) => !text.Contains('\'') ? "'" + text + "'" :
        !text.Contains('"') ? "\"" + text + "\"" : "concat('" + text.Replace("'", "',\"'\",'") + "')";

    public void Observe()
    {
        // Passive copies of the real UI's catalog GETs. No extra requests, request
        // bodies, credentials, tokens, or modified server responses.
        Browser.Script.ExecuteScript("""
            if (!window.__cqProviderCatalog) {
              const state = window.__cqProviderCatalog = {sequence:0, responses:[], writes:[]};
              const original = window.fetch;
              window.fetch = async function(...args) {
                const url = new URL(args[0] instanceof Request ? args[0].url : args[0], location.href);
                const method = String(args[1]?.method || args[0]?.method || 'GET').toUpperCase();
                const sequence = ++state.sequence;
                if (url.pathname.startsWith('/api/') && !['GET','HEAD','OPTIONS'].includes(method) &&
                    !(method === 'POST' && url.pathname === '/api/auth/login'))
                  state.writes.push({sequence, method, path:url.pathname});
                const response = await original.apply(this, args);
                if (method === 'GET' &&
                    /^\/api\/catalog\/(provider\/profile|accommodation-listings|restaurant-listings|activity-listings)$/.test(url.pathname)) {
                  const record = {sequence, url:response.url, status:response.status, body:null};
                  response.clone().json().then(body => {
                    record.body = body; state.responses.push(record);
                  }).catch(() => state.responses.push(record));
                }
                return response;
              };
            }
            """);
        ObservationInstalled = true;
    }

    public UiResponse Wait(string path) => Browser.Until(_ => {
        var json = Browser.Script.ExecuteScript("""
            if (!window.__cqProviderCatalog) throw new Error('Provider catalog observer lost after navigation');
            return JSON.stringify(window.__cqProviderCatalog.responses);
            """) as string ?? "[]";
        using var document = JsonDocument.Parse(json);
        var records = document.RootElement.EnumerateArray().Where(item =>
            new Uri(item.GetProperty("url").GetString()!).AbsolutePath == path)
            .OrderByDescending(item => item.GetProperty("sequence").GetInt32()).ToArray();
        if (records.Length == 0) return null!;
        var record = records[0];
        var status = record.GetProperty("status").GetInt32();
        Assert.True(status is >= 200 and < 300, $"Provider UI request {path} returned HTTP {status}.");
        Assert.NotEqual(JsonValueKind.Null, record.GetProperty("body").ValueKind);
        return new UiResponse(record.GetProperty("sequence").GetInt32(), record.Text("url"),
            status, record.GetProperty("body").Clone());
    }, "No successful real UI response for " + path + ". Check provider approval, service availability and deployment.");

    public void AssertNoBusinessWrites()
    {
        var writes = Browser.Script.ExecuteScript("""
            if (!window.__cqProviderCatalog) throw new Error('Provider catalog observer was lost');
            return JSON.stringify(window.__cqProviderCatalog.writes);
            """) as string;
        Assert.Equal("[]", writes);
    }

    public void ClickText(string xpathScope, string text) => Browser.Click(By.XPath(
        $"{xpathScope}//button[normalize-space()={Literal(text)}]"));

    public string Money(decimal amount) => "LKR " + (Browser.Script.ExecuteScript(
        "return Number(arguments[0]).toLocaleString()", (double)amount) as string
        ?? throw new InvalidOperationException("Browser money formatting failed."));

    public static string Number(decimal amount) => amount.ToString("0.############################", CultureInfo.InvariantCulture);
}

internal static class ProviderJson
{
    public static string Text(this JsonElement value, string property, string fallback = "") =>
        value.TryGetProperty(property, out var item) && item.ValueKind == JsonValueKind.String
            ? item.GetString() ?? fallback : fallback;
    public static int Int(this JsonElement value, string property) => value.GetProperty(property).GetInt32();
    public static decimal Money(this JsonElement value, string property) => value.GetProperty(property).GetDecimal();
    public static bool Flag(this JsonElement value, string property) => value.GetProperty(property).GetBoolean();
    public static string ListingName(this JsonElement value) => value.Text("title", value.Text("name", value.Text("roomType")));
}
