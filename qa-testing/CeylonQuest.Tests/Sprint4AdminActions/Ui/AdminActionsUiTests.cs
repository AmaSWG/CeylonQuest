using CeylonQuest.Tests.Sprint4AdminActions.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.Tests.Sprint4AdminActions.Ui;

[Collection(nameof(AdminUiCollection))]
[Trait("Story", "13.1")]
public sealed class AdminActionsUiTests
{
    private readonly AdminUiFixture _f;
    private IWebDriver Driver => _f.Driver;
    private WebDriverWait Wait => new(Driver, AdminTestConfig.EventualConsistencyTimeout) { PollingInterval = AdminTestConfig.PollInterval };

    public AdminActionsUiTests(AdminUiFixture fixture) => _f = fixture;

    private void LoginAs(string email, string password, string waitElementId = "nav-users")
    {
        Driver.Navigate().GoToUrl(AdminTestConfig.FrontendBaseUrl + "/?page=login");
        ((IJavaScriptExecutor)Driver).ExecuteScript("window.localStorage.clear(); window.sessionStorage.clear();");
        Driver.Navigate().GoToUrl(AdminTestConfig.FrontendBaseUrl + "/?page=login");
        Wait.Until(d => d.FindElements(By.Id("login-email")).Count > 0);
        Driver.FindElement(By.Id("login-email")).SendKeys(email);
        Driver.FindElement(By.Id("login-password")).SendKeys(password);
        Driver.FindElement(By.Id("login-button")).Click();
        Wait.Until(d => d.FindElements(By.Id(waitElementId)).Count > 0);
    }

    private void NavigateToAdminTab(string tabKey)
    {
        Wait.Until(d => d.FindElements(By.Id($"nav-{tabKey}")).Count > 0);
        Driver.FindElement(By.Id($"nav-{tabKey}")).Click();
    }

    [Fact(DisplayName = "TC-13.1-UI-01: Reactivate action for suspended users only")]
    public async Task Ui01_ReactivateActionOfferedOnlyForSuspendedUsers()
    {
        var suspended = await _f.Data.CreateSuspendedVisitorAsync("QA-UI01-Susp");
        var active = await _f.Data.CreateActiveVisitorAsync("QA-UI01-Actv");

        LoginAs(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        NavigateToAdminTab("users");

        // 1. Check suspended user
        var searchInput = Wait.Until(d => d.FindElement(By.CssSelector(".ad-search-input")));
        searchInput.Clear();
        searchInput.SendKeys(suspended.Email);

        Wait.Until(d => d.FindElements(By.XPath($"//tr[contains(., '{suspended.Email}')]")).Count > 0);
        var suspendedRow = Driver.FindElement(By.XPath($"//tr[contains(., '{suspended.Email}')]"));
        Assert.Equal("Suspended", suspendedRow.FindElement(By.CssSelector(".ad-badge--inactive")).Text.Trim(), ignoreCase: true);
        var reactivateBtn = suspendedRow.FindElement(By.CssSelector(".ad-row-btn--toggle"));
        Assert.Equal("Reactivate", reactivateBtn.Text.Trim());

        // 2. Check active user
        searchInput = Driver.FindElement(By.CssSelector(".ad-search-input"));
        searchInput.Clear();
        searchInput.SendKeys(active.Email);

        Wait.Until(d => d.FindElements(By.XPath($"//tr[contains(., '{active.Email}')]")).Count > 0);
        var activeRow = Driver.FindElement(By.XPath($"//tr[contains(., '{active.Email}')]"));
        Assert.Equal("Active", activeRow.FindElement(By.CssSelector(".ad-badge--active")).Text.Trim(), ignoreCase: true);
        var activeBtn = activeRow.FindElement(By.CssSelector(".ad-row-btn--toggle"));
        Assert.Equal("Suspend", activeBtn.Text.Trim());
    }

    [Fact(DisplayName = "TC-13.1-UI-02: S1 reactivate a suspended user")]
    public async Task Ui02_ReactivateSuspendedUserWithConfirmation()
    {
        var user = await _f.Data.CreateSuspendedVisitorAsync("QA-UI02");

        LoginAs(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        NavigateToAdminTab("users");

        var searchInput = Wait.Until(d => d.FindElement(By.CssSelector(".ad-search-input")));
        searchInput.Clear();
        searchInput.SendKeys(user.Email);

        var row = Wait.Until(d => d.FindElement(By.XPath($"//tr[contains(., '{user.Email}')]")));
        row.FindElement(By.CssSelector(".ad-row-btn--toggle")).Click();

        // Modal confirmation
        Wait.Until(d => d.FindElements(By.CssSelector(".cq-confirm-modal")).Count > 0);
        Assert.Equal("Reactivate User", Driver.FindElement(By.CssSelector(".cq-confirm-title")).Text.Trim());
        Driver.FindElement(By.CssSelector(".cq-confirm-btn--primary")).Click();

        // Verify toast and status change
        Wait.Until(d =>
        {
            var toasts = d.FindElements(By.CssSelector(".ad-toast--success, .ad-toast__msg"));
            return toasts.Any(t => t.Text.Contains("reactivated successfully", StringComparison.OrdinalIgnoreCase));
        });

        // Status badge updates to Active
        Wait.Until(d =>
        {
            var r = d.FindElements(By.XPath($"//tr[contains(., '{user.Email}')]"));
            return r.Count > 0 && r[0].FindElements(By.CssSelector(".ad-badge--active")).Count > 0;
        });
    }

    [Fact(DisplayName = "TC-13.1-UI-03: S1 reactivated user can log in")]
    public async Task Ui03_ReactivatedUserCanLogInWithExistingCredentials()
    {
        var user = await _f.Data.CreateSuspendedVisitorAsync("QA-UI03");

        // 1. Attempt login before reactivation -> blocked
        Driver.Navigate().GoToUrl(AdminTestConfig.FrontendBaseUrl + "/?page=login");
        ((IJavaScriptExecutor)Driver).ExecuteScript("window.localStorage.clear();");
        Driver.Navigate().GoToUrl(AdminTestConfig.FrontendBaseUrl + "/?page=login");
        Wait.Until(d => d.FindElements(By.Id("login-email")).Count > 0);
        Driver.FindElement(By.Id("login-email")).SendKeys(user.Email);
        Driver.FindElement(By.Id("login-password")).SendKeys(user.Password);
        Driver.FindElement(By.Id("login-button")).Click();

        // Assert error message shown
        Wait.Until(d => d.FindElements(By.CssSelector(".auth-error, [role='alert']")).Count > 0);
        Assert.Contains("not active", Driver.FindElement(By.CssSelector(".auth-error, [role='alert']")).Text);

        // 2. Admin reactivates user
        LoginAs(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        NavigateToAdminTab("users");

        var searchInput = Wait.Until(d => d.FindElement(By.CssSelector(".ad-search-input")));
        searchInput.Clear();
        searchInput.SendKeys(user.Email);

        var row = Wait.Until(d => d.FindElement(By.XPath($"//tr[contains(., '{user.Email}')]")));
        row.FindElement(By.CssSelector(".ad-row-btn--toggle")).Click();
        Wait.Until(d => d.FindElements(By.CssSelector(".cq-confirm-btn--primary")).Count > 0);
        Driver.FindElement(By.CssSelector(".cq-confirm-btn--primary")).Click();
        Wait.Until(d => d.FindElements(By.CssSelector(".ad-toast--success")).Count > 0);

        // 3. User logs in with original credentials -> succeeds
        Driver.Navigate().GoToUrl(AdminTestConfig.FrontendBaseUrl + "/?page=login");
        ((IJavaScriptExecutor)Driver).ExecuteScript("window.localStorage.clear();");
        Driver.Navigate().GoToUrl(AdminTestConfig.FrontendBaseUrl + "/?page=login");
        Wait.Until(d => d.FindElements(By.Id("login-email")).Count > 0);
        Driver.FindElement(By.Id("login-email")).SendKeys(user.Email);
        Driver.FindElement(By.Id("login-password")).SendKeys(user.Password);
        Driver.FindElement(By.Id("login-button")).Click();

        Wait.Until(d => d.FindElements(By.CssSelector(".cq-dashboard-wrapper, .cq-sidebar, #pd-logout-btn")).Count > 0);
    }

    [Fact(DisplayName = "TC-13.1-UI-04: S1 account data unchanged")]
    public async Task Ui04_AccountDataRemainsUnchangedAfterReactivation()
    {
        var user = await _f.Data.CreateSuspendedVisitorAsync("QA-UI04");

        LoginAs(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        NavigateToAdminTab("users");

        // Check data before
        var searchInput = Wait.Until(d => d.FindElement(By.CssSelector(".ad-search-input")));
        searchInput.Clear();
        searchInput.SendKeys(user.Email);

        var rowBefore = Wait.Until(d => d.FindElement(By.XPath($"//tr[contains(., '{user.Email}')]")));
        Assert.Contains(user.FirstName, rowBefore.Text);
        Assert.Contains(user.LastName, rowBefore.Text);
        Assert.Contains("Visitor", rowBefore.Text, StringComparison.OrdinalIgnoreCase);

        // Reactivate
        rowBefore.FindElement(By.CssSelector(".ad-row-btn--toggle")).Click();
        Wait.Until(d => d.FindElements(By.CssSelector(".cq-confirm-btn--primary")).Count > 0);
        Driver.FindElement(By.CssSelector(".cq-confirm-btn--primary")).Click();
        Wait.Until(d => d.FindElements(By.CssSelector(".ad-toast--success")).Count > 0);

        // Check data after
        searchInput = Driver.FindElement(By.CssSelector(".ad-search-input"));
        searchInput.Clear();
        searchInput.SendKeys(user.Email);

        var rowAfter = Wait.Until(d => d.FindElement(By.XPath($"//tr[contains(., '{user.Email}')]")));
        Assert.Contains(user.FirstName, rowAfter.Text);
        Assert.Contains(user.LastName, rowAfter.Text);
        Assert.Contains("Visitor", rowAfter.Text, StringComparison.OrdinalIgnoreCase);

        // Ensure no duplicate row exists
        Assert.Single(Driver.FindElements(By.XPath($"//tr[contains(., '{user.Email}')]")));
    }

    [Fact(DisplayName = "TC-13.1-UI-05: S2 already-active user - Reactivate action unavailable in UI")]
    public async Task Ui05_AlreadyActiveUserCannotBeReactivatedFromUi()
    {
        var activeUser = await _f.Data.CreateActiveVisitorAsync("QA-UI05");

        LoginAs(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        NavigateToAdminTab("users");

        var searchInput = Wait.Until(d => d.FindElement(By.CssSelector(".ad-search-input")));
        searchInput.Clear();
        searchInput.SendKeys(activeUser.Email);

        var row = Wait.Until(d => d.FindElement(By.XPath($"//tr[contains(., '{activeUser.Email}')]")));

        // Status is active
        Assert.Equal("Active", row.FindElement(By.CssSelector(".ad-badge--active")).Text.Trim(), ignoreCase: true);

        // Only Suspend is rendered, Reactivate is NOT offered in the UI
        var toggleBtn = row.FindElement(By.CssSelector(".ad-row-btn--toggle"));
        Assert.Equal("Suspend", toggleBtn.Text.Trim(), ignoreCase: true);

        var reactivateButtons = row.FindElements(By.XPath(".//button[contains(translate(text(), 'REACTIVATE', 'reactivate'), 'reactivate')]"));
        Assert.Empty(reactivateButtons);
    }

    [Fact(DisplayName = "TC-13.1-UI-06: S3 removal needs confirmation")]
    public async Task Ui06_RemovalNeedsConfirmationAndCancelKeepsListingActive()
    {
        var listing = await _f.Data.CreateActiveExperienceAsync("QA-UI06");

        LoginAs(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        NavigateToAdminTab("listings");

        var searchInput = Wait.Until(d => d.FindElement(By.CssSelector("input.ad-search-input")));
        searchInput.Clear();
        searchInput.SendKeys(listing.Title);

        var row = Wait.Until(d => d.FindElement(By.XPath($"//tr[contains(., '{listing.Title}')]")));
        row.FindElement(By.CssSelector(".ad-quick-btn--danger")).Click();

        // 1. Enter reason and click cancel on inline section
        Wait.Until(d => d.FindElements(By.Id("listing-removal-reason")).Count > 0);
        Driver.FindElement(By.Id("listing-removal-reason")).SendKeys("Test cancellation reason");
        Driver.FindElement(By.XPath("//button[text()='Cancel']")).Click();

        // Verify listing remains Active
        row = Driver.FindElement(By.XPath($"//tr[contains(., '{listing.Title}')]"));
        Assert.Equal("Active", row.FindElement(By.CssSelector(".ad-badge--active")).Text.Trim(), ignoreCase: true);

        // 2. Open inline section, continue to confirmation modal, and cancel on modal
        row.FindElement(By.CssSelector(".ad-quick-btn--danger")).Click();
        Wait.Until(d => d.FindElements(By.Id("listing-removal-reason")).Count > 0);
        Driver.FindElement(By.Id("listing-removal-reason")).SendKeys("Test cancellation reason 2");
        Driver.FindElement(By.XPath("//button[contains(text(), 'Continue to confirmation')]")).Click();

        Wait.Until(d => d.FindElements(By.CssSelector(".cq-confirm-modal")).Count > 0);
        Driver.FindElement(By.CssSelector(".cq-confirm-btn--cancel")).Click();

        // Verify still Active
        row = Driver.FindElement(By.XPath($"//tr[contains(., '{listing.Title}')]"));
        Assert.Equal("Active", row.FindElement(By.CssSelector(".ad-badge--active")).Text.Trim(), ignoreCase: true);
    }

    [Fact(DisplayName = "TC-13.1-UI-07: S3 confirm removal")]
    public async Task Ui07_ConfirmRemovalUpdatesListingStatusToRemoved()
    {
        var listing = await _f.Data.CreateActiveExperienceAsync("QA-UI07");

        LoginAs(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        NavigateToAdminTab("listings");

        var searchInput = Wait.Until(d => d.FindElement(By.CssSelector("input.ad-search-input")));
        searchInput.Clear();
        searchInput.SendKeys(listing.Title);

        var row = Wait.Until(d => d.FindElement(By.XPath($"//tr[contains(., '{listing.Title}')]")));
        row.FindElement(By.CssSelector(".ad-quick-btn--danger")).Click();

        Wait.Until(d => d.FindElements(By.Id("listing-removal-reason")).Count > 0);
        Driver.FindElement(By.Id("listing-removal-reason")).SendKeys("Violates quality guidelines");
        Driver.FindElement(By.XPath("//button[contains(text(), 'Continue to confirmation')]")).Click();

        Wait.Until(d => d.FindElements(By.CssSelector(".cq-confirm-modal")).Count > 0);
        Driver.FindElement(By.CssSelector(".cq-confirm-btn--danger")).Click();

        Wait.Until(d =>
        {
            var toasts = d.FindElements(By.CssSelector(".ad-toast--success, .ad-toast__msg"));
            return toasts.Any(t => t.Text.Contains("removed successfully", StringComparison.OrdinalIgnoreCase));
        });

        // Row now displays Removed status (case-insensitive to support uppercase badge CSS)
        Wait.Until(d =>
        {
            var r = d.FindElements(By.XPath($"//tr[contains(., '{listing.Title}')]"));
            return r.Count > 0 && r[0].Text.Contains("Removed", StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact(DisplayName = "TC-13.1-UI-08: S3 removed listing hidden from visitors")]
    public async Task Ui08_RemovedListingIsHiddenFromVisitorSearchAndDetail()
    {
        var listing = await _f.Data.CreateActiveExperienceAsync("QA-UI08");

        // 1. Remove listing as Admin via UI
        LoginAs(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        NavigateToAdminTab("listings");

        var searchInput = Wait.Until(d => d.FindElement(By.CssSelector("input.ad-search-input")));
        searchInput.Clear();
        searchInput.SendKeys(listing.Title);

        var row = Wait.Until(d => d.FindElement(By.XPath($"//tr[contains(., '{listing.Title}')]")));
        row.FindElement(By.CssSelector(".ad-quick-btn--danger")).Click();

        Wait.Until(d => d.FindElements(By.Id("listing-removal-reason")).Count > 0);
        Driver.FindElement(By.Id("listing-removal-reason")).SendKeys("Admin removed for QA test");
        Driver.FindElement(By.XPath("//button[contains(text(), 'Continue to confirmation')]")).Click();

        Wait.Until(d => d.FindElements(By.CssSelector(".cq-confirm-modal")).Count > 0);
        Driver.FindElement(By.CssSelector(".cq-confirm-btn--danger")).Click();
        Wait.Until(d => d.FindElements(By.CssSelector(".ad-toast--success")).Count > 0);

        // 2. Open visitor search and search for the listing title
        Driver.Navigate().GoToUrl(AdminTestConfig.FrontendBaseUrl + "/");
        ((IJavaScriptExecutor)Driver).ExecuteScript("window.localStorage.clear();");
        Driver.Navigate().GoToUrl(AdminTestConfig.FrontendBaseUrl + "/");

        // Public search API verify
        var searchResp = await _f.Api.SearchActivitiesRawAsync(listing.Title);
        searchResp.EnsureSuccessStatusCode();
        var searchContent = await searchResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain(listing.Id.ToString(), searchContent);

        // Public detail API verify
        var detailResp = await _f.Api.GetPublicActivityByIdRawAsync(listing.Id);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, detailResp.StatusCode);
    }

    [Fact(DisplayName = "TC-13.1-UI-09: S4 audit trail", Skip = "Audit view UI is not implemented in the Admin Dashboard.")]
    public void Ui09_AuditTrailViewDisplaysRecords()
    {
    }

    [Fact(DisplayName = "TC-13.1-UI-10: access control")]
    public async Task Ui10_NonAdminRolesCannotAccessAdminDashboard()
    {
        var visitor = await _f.Data.CreateActiveVisitorAsync("QA-UI10-Vis");

        // 1. Log in as Visitor
        Driver.Navigate().GoToUrl(AdminTestConfig.FrontendBaseUrl + "/?page=login");
        ((IJavaScriptExecutor)Driver).ExecuteScript("window.localStorage.clear();");
        Driver.Navigate().GoToUrl(AdminTestConfig.FrontendBaseUrl + "/?page=login");
        Wait.Until(d => d.FindElements(By.Id("login-email")).Count > 0);
        Driver.FindElement(By.Id("login-email")).SendKeys(visitor.Email);
        Driver.FindElement(By.Id("login-password")).SendKeys(visitor.Password);
        Driver.FindElement(By.Id("login-button")).Click();

        Wait.Until(d => d.FindElements(By.CssSelector(".cq-dashboard-wrapper, #pd-logout-btn")).Count > 0);

        // Assert admin navigation buttons are absent
        Assert.Empty(Driver.FindElements(By.Id("nav-users")));
        Assert.Empty(Driver.FindElements(By.Id("nav-listings")));

        // 2. Attempt navigating directly to admin-dashboard URL
        Driver.Navigate().GoToUrl(AdminTestConfig.FrontendBaseUrl + "/?page=admin-dashboard");
        Assert.Empty(Driver.FindElements(By.Id("nav-users")));
        Assert.Empty(Driver.FindElements(By.Id("nav-listings")));
    }
}
