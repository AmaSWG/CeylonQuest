using System.Text.Json;
using System.Text.RegularExpressions;
using CeylonQuest.E2E.Tests.Infrastructure;
using Xunit;

namespace CeylonQuest.E2E.Tests.Visitor.Infrastructure;

internal sealed record CatalogUiResponse(int Sequence, string Url, int Status, JsonElement Body);

internal sealed class VisitorCatalogUi(BrowserSession browser)
{
    public BrowserSession Browser { get; } = browser;
    public static string Normalize(string text) => Regex.Replace(text, @"\s+", " ").Trim();
    public static void EqualText(string expected, string actual, bool ignoreCase = false) =>
        Assert.Equal(Normalize(expected), Normalize(actual), ignoreCase: ignoreCase);
    public string Money(decimal amount) => "LKR " + (Browser.Script.ExecuteScript(
        "return Number(arguments[0]).toLocaleString()", (double)amount) as string
        ?? throw new InvalidOperationException("Browser currency formatting failed."));

    public void Observe()
    {
        // Observe copies of requests made by the real UI. Never issue requests, inject
        // responses, change authentication, or record credentials/request bodies.
        Browser.Script.ExecuteScript("""
            if (!window.__cqVisitorCatalog) {
              const state = window.__cqVisitorCatalog = {sequence: 0, responses: [], writes: []};
              const original = window.fetch;
              window.fetch = async function(...args) {
                const method = String(args[1]?.method || args[0]?.method || 'GET').toUpperCase();
                const url = new URL(args[0] instanceof Request ? args[0].url : args[0], location.href);
                const sequence = ++state.sequence;
                if (url.pathname.startsWith('/api/') && !['GET','HEAD','OPTIONS'].includes(method))
                  state.writes.push({sequence, method, path: url.pathname});
                const response = await original.apply(this, args);
                if (method === 'GET' && (url.pathname === '/api/catalog/search' ||
                    /^\/api\/catalog\/availability\/[0-9a-f-]+$/i.test(url.pathname))) {
                  const record = {sequence, url: response.url, status: response.status, body: null};
                  response.clone().json().then(body => {
                    record.body = body; state.responses.push(record);
                  }).catch(() => state.responses.push(record));
                }
                return response;
              };
            }
            """);
    }

    public int Sequence => Convert.ToInt32(Browser.Script.ExecuteScript("return window.__cqVisitorCatalog?.sequence || 0"));

    public CatalogUiResponse WaitResponse(string path, int after = -1, string? date = null)
    {
        return Browser.Until(_ => {
            var json = Browser.Script.ExecuteScript("""
                if (!window.__cqVisitorCatalog) throw new Error('Catalog observation lost after navigation');
                return JSON.stringify(window.__cqVisitorCatalog.responses);
                """) as string ?? "[]";
            using var document = JsonDocument.Parse(json);
            var records = document.RootElement.EnumerateArray().Where(item => {
                var uri = new Uri(item.GetProperty("url").GetString()!);
                return item.GetProperty("sequence").GetInt32() > after && uri.AbsolutePath == path &&
                    (date is null || Query(uri, "date") == date);
            }).OrderByDescending(item => item.GetProperty("sequence").GetInt32()).ToArray();
            if (records.Length == 0) return null!;
            var record = records[0];
            var status = record.GetProperty("status").GetInt32();
            Assert.True(status is >= 200 and < 300, $"UI request {path} returned HTTP {status}.");
            Assert.NotEqual(JsonValueKind.Null, record.GetProperty("body").ValueKind);
            return new CatalogUiResponse(record.GetProperty("sequence").GetInt32(), record.GetProperty("url").GetString()!,
                status, record.GetProperty("body").Clone());
        }, "No successful real UI response for " + path + (date is null ? "" : " on " + date));
    }

    private static string Query(Uri uri, string name) => uri.Query.TrimStart('?').Split('&')
        .Select(part => part.Split('=', 2)).Where(part => Uri.UnescapeDataString(part[0]) == name)
        .Select(part => part.Length > 1 ? Uri.UnescapeDataString(part[1]) : "").FirstOrDefault() ?? "";

    public void AssertNoBusinessWrites()
    {
        var writes = Browser.Script.ExecuteScript("""
            if (!window.__cqVisitorCatalog) throw new Error('Catalog observation lost');
            return JSON.stringify(window.__cqVisitorCatalog.writes);
            """) as string;
        Assert.Equal("[]", writes);
    }

}
