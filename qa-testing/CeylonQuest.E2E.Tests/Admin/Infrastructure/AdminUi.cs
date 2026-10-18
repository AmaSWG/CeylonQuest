using System.Globalization;
using System.Text.Json;
using CeylonQuest.E2E.Tests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.E2E.Tests.Admin.Infrastructure;

internal sealed record AdminRead(int Sequence, string Url, int Status, JsonElement Body,
    string ContentType, string Disposition, long ByteLength);

internal sealed class AdminUi(BrowserSession browser)
{
    public BrowserSession Browser => browser;
    public bool Observing { get; private set; }
    public int Sequence => Convert.ToInt32(browser.Script.ExecuteScript("return window.__cqAdminCatalog?.sequence || 0"));
    public static string Normalize(string value) => string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    public static bool SameText(string expected, string actual, bool ignoreCase = false) =>
        Normalize(expected).Equals(Normalize(actual), ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    public static void EqualText(string expected, string actual, bool ignoreCase = false) =>
        Assert.Equal(Normalize(expected), Normalize(actual), ignoreCase: ignoreCase);
    public static string Literal(string value) => "concat('" + value.Replace("'", "',\"'\",'") + "','')";

    public void Observe()
    {
        browser.Script.ExecuteScript("""
            if (!window.__cqAdminCatalog) {
              const state = window.__cqAdminCatalog = {sequence:0, reads:[], writes:[]};
              const original = window.fetch;
              window.fetch = async function(...args) {
                const url = new URL(args[0] instanceof Request ? args[0].url : args[0], location.href);
                const method = String(args[1]?.method || args[0]?.method || 'GET').toUpperCase();
                const sequence = ++state.sequence;
                if (url.pathname.startsWith('/api/') && !['GET','HEAD','OPTIONS'].includes(method) &&
                    !(method === 'POST' && url.pathname === '/api/auth/login'))
                  state.writes.push({sequence, path:url.pathname, method});
                const response = await original.apply(this,args);
                const binary = /^\/api\/catalog\/admin\/providers\/[0-9a-f-]+\/document$/i.test(url.pathname);
                const watch = url.pathname === '/api/catalog/admin/providers' ||
                  url.pathname === '/api/catalog/reports/admin-listings' ||
                  url.pathname === '/api/catalog/reports/inventory-summary' || binary;
                if (method === 'GET' && watch) {
                  const item = {sequence, url:response.url, status:response.status, body:null, ready:false,
                    contentType:response.headers.get('Content-Type') || '',
                    disposition:response.headers.get('Content-Disposition') || '', byteLength:0};
                  state.reads.push(item);
                  if (binary) response.clone().arrayBuffer().then(body => {
                    item.byteLength=body.byteLength; item.ready=true;
                  }).catch(() => item.ready=true);
                  else response.clone().json().then(body => {
                    item.body=body; item.ready=true;
                  }).catch(() => item.ready=true);
                }
                return response;
              };
            }
            """);
        Observing = true;
    }

    public AdminRead Wait(string path, int after = -1, Func<Uri, bool>? query = null, bool binary = false) =>
        browser.Until(_ => {
            var json = browser.Script.ExecuteScript("""
                if (!window.__cqAdminCatalog) throw new Error('Admin catalog observation lost after navigation');
                return JSON.stringify(window.__cqAdminCatalog.reads);
                """) as string ?? "[]";
            using var document = JsonDocument.Parse(json);
            var records = document.RootElement.EnumerateArray().Where(item =>
                item.GetProperty("sequence").GetInt32() > after &&
                new Uri(item.Text("url")).AbsolutePath == path &&
                (query is null || query(new Uri(item.Text("url")))))
                .OrderByDescending(item => item.GetProperty("sequence").GetInt32()).ToArray();
            if (records.Length == 0) return null!;
            var record = records[0];
            var status = record.GetProperty("status").GetInt32();
            Assert.True(status is >= 200 and < 300, $"Admin UI GET {path} returned HTTP {status}.");
            if (!record.GetProperty("ready").GetBoolean()) return null!;
            if (!binary) Assert.NotEqual(JsonValueKind.Null, record.GetProperty("body").ValueKind);
            return new AdminRead(record.GetProperty("sequence").GetInt32(), record.Text("url"), status,
                record.GetProperty("body").Clone(), record.Text("contentType"), record.Text("disposition"),
                record.GetProperty("byteLength").GetInt64());
        }, "Real Admin UI response missing or incomplete: " + path);

    public void NoWrites()
    {
        var json = browser.Script.ExecuteScript("""
            if (!window.__cqAdminCatalog) throw new Error('Admin catalog observer was lost');
            return JSON.stringify(window.__cqAdminCatalog.writes);
            """) as string;
        Assert.Equal("[]", json);
    }

    public static string Query(Uri uri, string name) => uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Split('=', 2)).Where(part => Uri.UnescapeDataString(part[0]) == name)
        .Select(part => part.Length == 2 ? Uri.UnescapeDataString(part[1].Replace('+', ' ')) : "").FirstOrDefault() ?? "";

    public void Heading(string expected) => browser.Until(driver => driver.FindElements(By.CssSelector("h1"))
        .Any(element => element.Displayed && SameText(expected, element.Text)), "Admin heading did not appear: " + expected);
    public void Button(string text, string scope) => browser.Click(By.XPath(scope +
        "//button[normalize-space(.)=" + Literal(text) + "]"));
    public void Select(string id, string value) => new SelectElement(browser.Visible(By.Id(id))).SelectByValue(value);
    public IReadOnlyList<IWebElement> Rows(string scope) => browser.Driver.FindElements(By.CssSelector(scope + " tbody tr"))
        .Where(element => element.Displayed).ToArray();
    public string[][] Table(string scope) => JsonSerializer.Deserialize<string[][]>((string)browser.Script.ExecuteScript("""
        return JSON.stringify(Array.from(document.querySelectorAll(arguments[0] + ' tbody tr'))
          .filter(row => row.getClientRects().length).map(row => Array.from(row.cells).map(cell => cell.innerText.trim())));
        """, scope)!)!;
    public void NoErrors(string scope) => Assert.DoesNotContain(browser.Driver.FindElements(By.CssSelector(
        scope + " [role='alert'], " + scope + " .cq-report-alert--danger")), element => element.Displayed);

    public void InputValue(By selector, string expected)
    {
        var element = browser.Visible(selector);
        var actual = element.GetDomProperty("value");
        Assert.NotNull(actual);
        if (element.TagName.Equals("textarea", StringComparison.OrdinalIgnoreCase))
        {
            expected = expected.Replace("\r\n", "\n").Replace('\r', '\n');
            actual = actual.Replace("\r\n", "\n").Replace('\r', '\n');
        }
        Assert.Equal(expected, actual);
    }
    public static string Number(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);
}

internal static class AdminJson
{
    public static string Text(this JsonElement item, string name, string fallback = "") =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : fallback;
    public static int Int(this JsonElement item, string name) => item.GetProperty(name).GetInt32();
    public static decimal Number(this JsonElement item, string name) => item.GetProperty(name).GetDecimal();
    public static bool Flag(this JsonElement item, string name) => item.GetProperty(name).GetBoolean();
    public static JsonElement[] Array(this JsonElement item, string name) =>
        item.GetProperty(name).EnumerateArray().Select(value => value.Clone()).ToArray();
}
