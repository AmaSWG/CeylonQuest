using System.Net;
using CeylonQuest.Tests.Sprint4ProviderPerformanceReport.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.Tests.Sprint4ProviderPerformanceReport.Ui;

[Collection(nameof(PerformanceReportUiCollection))]
[Trait("Story", "13.2")]
[Trait("Category", "Ui")]
public sealed class ProviderPerformanceReportUiTests
{
    private readonly PerformanceReportUiFixture _f;
    private IWebDriver Driver => _f.Driver;
    private WebDriverWait Wait => new(Driver, PerformanceReportTestConfig.EventualConsistencyTimeout)
    {
        PollingInterval = PerformanceReportTestConfig.PollInterval
    };

    public ProviderPerformanceReportUiTests(PerformanceReportUiFixture fixture) => _f = fixture;

    private const string UiNotImplementedSkipReason =
        "Provider Performance & Reviews Report UI is not implemented yet in the Admin Dashboard (pending frontend implementation).";

    [Fact(DisplayName = "TC-14.1-UI-01: S1: Report loads for admin", Skip = UiNotImplementedSkipReason)]
    public void Ui01_ReportLoadsForAdmin()
    {
    }

    [Fact(DisplayName = "TC-14.1-UI-02: S1: Columns shown", Skip = UiNotImplementedSkipReason)]
    public void Ui02_ColumnsShown()
    {
    }

    [Fact(DisplayName = "TC-14.1-UI-03: S1: Values match known data", Skip = UiNotImplementedSkipReason)]
    public void Ui03_ValuesMatchKnownData()
    {
    }

    [Fact(DisplayName = "TC-14.1-UI-04: S2: Low Rating indicator", Skip = UiNotImplementedSkipReason)]
    public void Ui04_LowRatingIndicator()
    {
    }

    [Fact(DisplayName = "TC-14.1-UI-05: S2: No Reviews indicator", Skip = UiNotImplementedSkipReason)]
    public void Ui05_NoReviewsIndicator()
    {
    }

    [Fact(DisplayName = "TC-14.1-UI-06: S3: Date range filter", Skip = UiNotImplementedSkipReason)]
    public void Ui06_DateRangeFilter()
    {
    }

    [Fact(DisplayName = "TC-14.1-UI-07: S3: Category filter", Skip = UiNotImplementedSkipReason)]
    public void Ui07_CategoryFilter()
    {
    }

    [Fact(DisplayName = "TC-14.1-UI-08: S3: Minimum rating filter", Skip = UiNotImplementedSkipReason)]
    public void Ui08_MinimumRatingFilter()
    {
    }

    [Fact(DisplayName = "TC-14.1-UI-09: S3: Combined filters and empty state", Skip = UiNotImplementedSkipReason)]
    public void Ui09_CombinedFiltersAndEmptyState()
    {
    }

    [Fact(DisplayName = "TC-14.1-UI-10: S4: Sort by Average Rating", Skip = UiNotImplementedSkipReason)]
    public void Ui10_SortByAverageRating()
    {
    }

    [Fact(DisplayName = "TC-14.1-UI-11: Access control")]
    public async Task Ui11_AccessControl_NonAdminRolesCannotAccessReportOrApi()
    {
        // 1. Verify Visitor role cannot access report API
        var (_, _, visitorToken) = await _f.Api.CreateVisitorAsync("QA-UI11-Vis");
        using var visitorApiResp = await _f.Api.GetReportRawAsync(visitorToken);
        Assert.Equal(HttpStatusCode.Forbidden, visitorApiResp.StatusCode);

        // 2. Verify Provider role cannot access report API
        var (providerToken, _) = await _f.Api.LoginAsync(
            PerformanceReportTestConfig.ProviderEmail,
            PerformanceReportTestConfig.ProviderPassword);
        using var providerApiResp = await _f.Api.GetReportRawAsync(providerToken);
        Assert.Equal(HttpStatusCode.Forbidden, providerApiResp.StatusCode);

        // 3. Verify Visitor UI does not offer admin report navigation
        Driver.Navigate().GoToUrl(PerformanceReportTestConfig.FrontendBaseUrl + "/?page=login");
        ((IJavaScriptExecutor)Driver).ExecuteScript("window.localStorage.clear();");
        Driver.Navigate().GoToUrl(PerformanceReportTestConfig.FrontendBaseUrl + "/?page=login");

        Wait.Until(d => d.FindElements(By.Id("login-email")).Count > 0);
        Driver.FindElement(By.Id("login-email")).SendKeys(PerformanceReportTestConfig.ProviderEmail);
        Driver.FindElement(By.Id("login-password")).SendKeys(PerformanceReportTestConfig.ProviderPassword);
        Driver.FindElement(By.Id("login-button")).Click();

        // Wait for Provider Dashboard to load
        Wait.Until(d => d.FindElements(By.CssSelector(".pd-sidebar, .cq-sidebar, #pd-logout-btn")).Count > 0);

        // Confirm admin performance report is not present in provider navigation
        var adminReportNav = Driver.FindElements(By.XPath("//button[contains(., 'Provider Performance')] | //a[contains(., 'Provider Performance')]"));
        Assert.Empty(adminReportNav);
    }
}
