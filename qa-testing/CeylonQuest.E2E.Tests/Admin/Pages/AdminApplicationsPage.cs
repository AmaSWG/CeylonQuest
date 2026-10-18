using System.Text.Json;
using System.Text.RegularExpressions;
using CeylonQuest.E2E.Tests.Admin.Infrastructure;
using CeylonQuest.E2E.Tests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.E2E.Tests.Admin.Pages;

internal sealed record ApplicationDocument(int Index, string Label, string DownloadName);

internal sealed class AdminApplicationsPage(AdminUi ui, AdminDashboardPage dashboard)
{
    private const string Scope = ".ad-applications-tab";
    private static readonly By Search = By.CssSelector(Scope + " .ad-search-input");
    public JsonElement[] Records => dashboard.Applications;

    public void Open()
    {
        dashboard.Open("applications", "Provider Applications");
        ui.Browser.Visible(By.CssSelector(Scope));
        AssertRows(Records);
        AssertCounts();
    }

    private static string Applicant(JsonElement app) => string.IsNullOrWhiteSpace(app.Text("firstName") + app.Text("lastName"))
        ? "Business Contact" : (app.Text("firstName") + " " + app.Text("lastName")).Trim();

    private static bool MatchesRow(string[] row, JsonElement app) => row.Length == 7 &&
        AdminUi.SameText(app.Text("businessName"), row[0]) &&
        AdminUi.SameText(Applicant(app) + " " + app.Text("email"), row[1]) &&
        AdminUi.SameText(app.Text("serviceType"), row[2]) &&
        AdminUi.SameText(app.Text("location"), row[3]) &&
        AdminUi.SameText(app.Text("status"), row[5], ignoreCase: true);

    public void AssertRows(IReadOnlyList<JsonElement> expected)
    {
        ui.Browser.Until(_ => {
            var actual = ui.Table(Scope);
            return actual.Length == expected.Count && actual.Select((row, index) => MatchesRow(row, expected[index])).All(match => match);
        }, "Application rows differ from the selected catalog records (business/contact/category/location/status).");
        if (expected.Count == 0)
            AdminUi.EqualText("No applications found", ui.Browser.Visible(By.CssSelector(Scope + " .ad-empty__title")).Text);
        else
        {
            foreach (var row in ui.Rows(Scope))
                Assert.True(row.FindElement(By.CssSelector(".ad-row-btn--view")).Enabled);
        }
        // The UI currently uses createdAt while this API projects submittedAt.
        // Submitted-date formatting is outside these business/status assertions.
    }

    private void AssertCounts()
    {
        var expected = new[] { $"All ({Records.Length})" }.Concat(new[] { "Pending", "Approved", "Rejected" }
            .Select(status => $"{status} ({Records.Count(app => app.Text("status").Equals(status, StringComparison.OrdinalIgnoreCase))})")).ToArray();
        var actual = ui.Browser.Driver.FindElements(By.CssSelector(Scope + " .ad-filter-pill")).Select(element => element.Text).ToArray();
        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < expected.Length; index++) AdminUi.EqualText(expected[index], actual[index], ignoreCase: true);
    }

    public void Status(string status)
    {
        var count = status == "All" ? Records.Length : Records.Count(app => app.Text("status").Equals(status, StringComparison.OrdinalIgnoreCase));
        ui.Button($"{status} ({count})", "//div[contains(@class,'ad-applications-tab')]//div[contains(@class,'ad-filter-pills')]");
        ui.Browser.Until(_ => AdminUi.SameText($"{status} ({count})",
            ui.Browser.Visible(By.CssSelector(Scope + " .ad-filter-pill.active")).Text, ignoreCase: true), "Application status selection did not update.");
    }

    public void AssertStatus(string status)
    {
        Status(status);
        AssertRows(status == "All" ? Records : Records.Where(app => app.Text("status").Equals(status, StringComparison.OrdinalIgnoreCase)).ToArray());
        AssertCounts();
    }

    public void Reset()
    {
        ui.Browser.Fill(Search, "");
        Status("All");
        AssertRows(Records);
    }

    public JsonElement Choose(string purpose = "details")
    {
        var variable = purpose switch {
            "pending" => "CQ_E2E_ADMIN_CATALOG_PENDING_APPLICATION_ID",
            "document" => "CQ_E2E_ADMIN_CATALOG_DOCUMENT_APPLICATION_ID",
            _ => "CQ_E2E_ADMIN_CATALOG_APPLICATION_ID"
        };
        var id = AdminSettings.OptionalId(variable);
        var candidates = Records.Where(app => (id is null || app.Text("id").Equals(id, StringComparison.OrdinalIgnoreCase)) &&
            (purpose != "pending" || app.Text("status").Equals("Pending", StringComparison.OrdinalIgnoreCase)) &&
            (purpose != "document" || HasDocument(app))).ToArray();
        Assert.True(candidates.Length > 0, $"Catalog fixture missing: {purpose} application. Set {variable} to a suitable existing record or provide matching test data.");
        if (id is not null) Assert.Single(candidates);
        ui.Browser.Step("Selected " + purpose + " application ID: " + candidates[0].Text("id"));
        return candidates[0];
    }

    public void SearchAndClear(string property)
    {
        var app = Choose();
        var query = app.Text(property).ToUpperInvariant();
        Assert.False(string.IsNullOrWhiteSpace(query));
        ui.Browser.Fill(Search, query);
        var expected = Records.Where(item => new[] { "businessName", "firstName", "lastName", "email", "serviceType", "location" }
            .Any(field => item.Text(field).Contains(query, StringComparison.OrdinalIgnoreCase))).ToArray();
        Assert.Contains(expected, item => item.Text("id") == app.Text("id"));
        AssertRows(expected);
        ui.Browser.Fill(Search, "");
        AssertRows(Records);
    }

    public void OpenReview(JsonElement app)
    {
        Reset();
        var index = Array.FindIndex(Records, item => item.Text("id") == app.Text("id"));
        Assert.True(index >= 0);
        ui.Rows(Scope)[index].FindElement(By.CssSelector(".ad-row-btn--view")).Click();
        ui.Browser.Visible(By.CssSelector(Scope + " .ad-modal[role='dialog']"));
        AssertReview(app);
    }

    public void AssertReview(JsonElement app)
    {
        var dialog = ui.Browser.Visible(By.CssSelector(Scope + " .ad-modal"));
        AdminUi.EqualText("Application Details: " + app.Text("businessName"), dialog.FindElement(By.CssSelector(".ad-modal__title")).Text);
        AdminUi.EqualText(app.Text("status"), dialog.FindElement(By.CssSelector(".ad-badge")).Text, ignoreCase: true);
        var fields = dialog.FindElements(By.CssSelector(".ad-field"));
        var personal = string.IsNullOrWhiteSpace(app.Text("firstName") + app.Text("lastName"))
            ? "To be completed by Provider via OTP activation" : (app.Text("firstName") + " " + app.Text("lastName")).Trim();
        foreach (var (label, expected) in new[] {
            ("Business / Contact", Default(app, "businessName", "Business Application")),
            ("Contact Email", app.Text("email")), ("Applicant Personal Details", personal),
            ("Contact Phone", Default(app, "phoneNumber", "Completed upon OTP activation")),
            ("Service Category", app.Text("serviceType")), ("Operating Location", app.Text("location")),
            ("Business Description", Default(app, "description", "No description provided."))
        })
        {
            var field = Assert.Single(fields, element => AdminUi.SameText(label,
                element.FindElement(By.CssSelector(".ad-field__label")).Text, ignoreCase: true));
            AdminUi.EqualText(expected, field.FindElement(By.CssSelector(".ad-field__value")).Text);
        }
        var documents = Documents(app);
        var docField = Assert.Single(fields, element => AdminUi.Normalize(element.FindElement(By.CssSelector(".ad-field__label")).Text)
            .StartsWith("Legal & Registration Documents", StringComparison.OrdinalIgnoreCase));
        AdminUi.EqualText($"Legal & Registration Documents ({documents.Length} files)",
            docField.FindElement(By.CssSelector(".ad-field__label")).Text, ignoreCase: true);
        var labels = docField.FindElements(By.TagName("strong")).Select(element => element.Text).ToArray();
        Assert.Equal(documents.Select(document => document.Label).ToArray(), labels);
        Assert.Equal(documents.Length, docField.FindElements(By.TagName("button")).Count);
    }

    private static string Default(JsonElement item, string key, string fallback) => item.Text(key) is { Length: > 0 } value ? value : fallback;

    private static ApplicationDocument[] Documents(JsonElement app)
    {
        var fallbackDownload = Default(app, "legalDocumentFileName", Regex.Replace(Default(app, "businessName", "application"), @"\s+", "_") + "_document.pdf");
        var raw = app.Text("legalDocumentsJson", "[]");
        try
        {
            using var metadata = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "[]" : raw);
            if (metadata.RootElement.ValueKind == JsonValueKind.Array && metadata.RootElement.GetArrayLength() > 0)
                return metadata.RootElement.EnumerateArray().Select((document, index) =>
                    new ApplicationDocument(index, Default(document, "OriginalFileName", $"Document #{index + 1}"),
                        Default(document, "OriginalFileName", fallbackDownload))).ToArray();
        }
        catch (JsonException) { /* Match the UI's single-document legacy fallback. */ }
        return [new ApplicationDocument(0, Default(app, "legalDocumentFileName", "Standard Registration Record"), fallbackDownload)];
    }

    private static bool HasDocument(JsonElement app)
    {
        if (!string.IsNullOrWhiteSpace(app.Text("legalDocumentFileName"))) return true;
        try
        {
            using var metadata = JsonDocument.Parse(app.Text("legalDocumentsJson", "[]"));
            return metadata.RootElement.ValueKind == JsonValueKind.Array && metadata.RootElement.GetArrayLength() > 0;
        }
        catch (JsonException) { return false; }
    }

    public void CloseReview()
    {
        ui.Button("Close Review", "//div[contains(@class,'ad-applications-tab')]//div[contains(@class,'ad-modal__body')]");
        ui.Browser.Until(driver => !driver.FindElements(By.CssSelector(Scope + " .ad-modal")).Any(element => element.Displayed),
            "Application review did not close.");
    }

    public void CancelApproval(JsonElement app)
    {
        OpenReview(app);
        ui.Button("Approve", "//div[contains(@class,'ad-applications-tab')]//div[contains(@class,'ad-modal__body')]");
        var confirm = ui.Browser.Visible(By.CssSelector(Scope + " .cq-confirm-modal"));
        AdminUi.EqualText("Approve Provider Application", confirm.FindElement(By.CssSelector(".cq-confirm-title")).Text);
        Assert.Contains(app.Text("businessName"), confirm.FindElement(By.CssSelector(".cq-confirm-message")).Text);
        ui.Browser.Click(By.CssSelector(Scope + " .cq-confirm-btn--cancel"));
        ui.Browser.Until(driver => !driver.FindElements(By.CssSelector(Scope + " .cq-confirm-modal")).Any(element => element.Displayed),
            "Approval confirmation did not close after Cancel.");
        AssertReview(app);
        CloseReview();
        AssertRows(Records);
        ui.NoWrites();
    }

    public void CancelRejection(JsonElement app)
    {
        OpenReview(app);
        ui.Button("Reject", "//div[contains(@class,'ad-applications-tab')]//div[contains(@class,'ad-modal__body')]");
        var form = ui.Browser.Visible(By.CssSelector(".ad-confirm-reject-form"));
        var reasons = new SelectElement(form.FindElement(By.TagName("select")));
        var reason = reasons.Options.First(option => !string.IsNullOrEmpty(option.GetDomAttribute("value")) && option.GetDomAttribute("value") != "other")
            .GetDomAttribute("value")!;
        reasons.SelectByValue(reason);
        var feedback = "Valid review feedback entered for cancellation.\nNo rejection should be submitted.";
        var feedbackSelector = By.CssSelector(".ad-confirm-reject-form textarea");
        ui.Browser.Fill(feedbackSelector, feedback);
        ui.InputValue(feedbackSelector, feedback);
        Assert.Equal(reason, new SelectElement(form.FindElement(By.TagName("select"))).SelectedOption.GetDomAttribute("value"));
        Assert.True(form.FindElements(By.TagName("button")).Single(button => AdminUi.SameText("Reject Application", button.Text, ignoreCase: true)).Enabled);
        ui.Button("Cancel", "//div[contains(@class,'ad-confirm-reject-form')]");
        ui.Browser.Until(driver => !driver.FindElements(By.CssSelector(".ad-confirm-reject-form")).Any(element => element.Displayed),
            "Rejection draft did not close after Cancel.");
        AssertReview(app);
        CloseReview();
        AssertRows(Records);
        ui.NoWrites();
    }

    public void DownloadDocument(JsonElement app)
    {
        OpenReview(app);
        var document = Documents(app).SingleOrDefault(document => document.Index == AdminSettings.DocumentIndex);
        Assert.NotNull(document);
        // Resolve from the test assembly, so direct dotnet and IDE execution agree.
        var downloadDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Admin", "TestResults", "downloads", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(downloadDirectory);
        Assert.IsType<ChromeDriver>(ui.Browser.Driver).ExecuteCdpCommand("Browser.setDownloadBehavior",
            new Dictionary<string, object?> { ["behavior"] = "allow", ["downloadPath"] = downloadDirectory });
        var baseline = ui.Sequence;
        var buttons = ui.Browser.Driver.FindElements(By.CssSelector(Scope + " .ad-modal button"))
            .Where(button => AdminUi.SameText("Download Document", button.Text)).ToArray();
        Assert.Equal(Documents(app).Length, buttons.Length);
        buttons[document.Index].Click();
        var response = ui.Wait(AdminDashboardPage.ApplicationsPath + "/" + app.Text("id") + "/document", baseline,
            uri => AdminUi.Query(uri, "index") == document.Index.ToString(), binary: true);
        Assert.True(response.ByteLength > 0, "Verification document response was empty.");
        Assert.DoesNotContain("text/html", response.ContentType.ToLowerInvariant());
        var filename = document.DownloadName;
        var dispositionName = Regex.Match(response.Disposition, "filename=\"?([^\";]+)\"?");
        if (dispositionName.Success) filename = dispositionName.Groups[1].Value;
        var expectedFilename = Path.GetFileName(filename);
        var file = ui.Browser.Until(_ => {
            var files = Directory.GetFiles(downloadDirectory).Where(path => !path.EndsWith(".crdownload", StringComparison.OrdinalIgnoreCase)).ToArray();
            return files.Length == 1 && new FileInfo(files[0]).Length == response.ByteLength ? files[0] : null!;
        }, "The application's document download did not complete with the expected byte length.");
        Assert.Equal(expectedFilename, Path.GetFileName(file));
        var bytes = File.ReadAllBytes(file);
        switch (Path.GetExtension(file).ToLowerInvariant())
        {
            case ".pdf": Assert.True(bytes.AsSpan().StartsWith("%PDF-"u8), "Downloaded file is not a PDF document."); break;
            case ".png": Assert.True(bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }), "Downloaded PNG signature is invalid."); break;
            case ".jpg": case ".jpeg": Assert.True(bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 216, "Downloaded JPEG signature is invalid."); break;
        }
        ui.Browser.Step($"Verification document download checked: {expectedFilename}, {bytes.Length} bytes. File retained in the ignored Admin TestResults/downloads directory.");
        CloseReview();
        ui.NoWrites();
    }
}
