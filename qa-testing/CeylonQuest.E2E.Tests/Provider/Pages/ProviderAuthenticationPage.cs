using System.Security.Cryptography;
using CeylonQuest.E2E.Tests.Infrastructure;
using CeylonQuest.E2E.Tests.Pages;
using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using OpenQA.Selenium;
using Xunit;

namespace CeylonQuest.E2E.Tests.Provider.Pages;

internal sealed class ProviderAuthenticationPage(BrowserSession browser)
{
    private readonly LoginPage login = new(browser);
    private static readonly string[] ProviderOnlyNavigation = ["overview", "business", "services", "reviews", "account"];

    public void VerifyIncorrectPasswordRejected()
    {
        var email = TestSettings.Required("CQ_E2E_PROVIDER_EMAIL");
        var validPassword = TestSettings.Required("CQ_E2E_PROVIDER_PASSWORD");

        // Verify the account really is an active Provider before the negative
        // attempt. An unregistered/inactive account must not produce a false pass.
        login.OpenFromHome();
        login.Submit(email, validPassword);
        browser.Visible(By.Id("nav-overview"));
        Assert.Equal("Provider", browser.Script.ExecuteScript("return localStorage.getItem('userRole')"));
        Assert.True(browser.Script.ExecuteScript("return Boolean(localStorage.getItem('authToken'))") is true);
        browser.Visible(By.Id("nav-services"));
        browser.Click(By.Id("pd-logout-btn"));
        browser.Visible(By.Id("nav-login-btn"));
        AssertNoSession();

        string wrongPassword;
        do { wrongPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)); }
        while (string.Equals(wrongPassword, validPassword, StringComparison.Ordinal));

        login.OpenFromHome();
        login.Submit(email, wrongPassword); // One incorrect attempt; no hardcoded password.
        Assert.Contains("invalid", login.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(email, browser.Visible(By.Id("login-email")).GetDomProperty("value"));
        browser.Visible(By.Id("login-password"));
        browser.Visible(By.Id("login-button"));
        AssertNoSession();
        AssertNoProviderNavigation();
        browser.Capture("provider-incorrect-password");
        browser.Step("Incorrect password rejected for a previously verified Provider; login page retained and no authenticated session created.");
    }

    public void VerifyVisitorCannotAccessProvider()
    {
        // There is one shared login page. Valid Visitor credentials should create
        // a Visitor session, not show an incorrect-password error or a Provider UI.
        login.LoginVisitor();
        AssertVisitorBoundary();
        browser.Driver.Navigate().GoToUrl(TestSettings.BaseUrl);
        AssertVisitorBoundary();

        // No /provider/dashboard route exists. Verify the actual UI role-routing
        // boundary, then supplement it with the implemented Provider-only GET.
        // Discover the Booking service origin from the Visitor UI's real request,
        // so a staging deployment is never sent to a hardcoded production API.
        browser.Click(By.Id("nav-bookings"));
        var visitorRequest = browser.Until(_ => browser.Script.ExecuteScript("""
            return performance.getEntriesByType('resource').map(entry => entry.name)
              .reverse().find(name => new URL(name).pathname === '/api/user-bookings/my') || null;
            """) as string!, "Visitor UI did not request its bookings; cannot identify the actual Booking service.");
        ArgumentException.ThrowIfNullOrWhiteSpace(visitorRequest);
        var providerReadUrl = new Uri(new Uri(visitorRequest), "/api/provider-bookings/my").AbsoluteUri;
        var status = Convert.ToInt32(browser.Script.ExecuteAsyncScript("""
            const url = arguments[0], done = arguments[arguments.length - 1];
            const token = localStorage.getItem('authToken');
            if (!token) { done(-1); return; }
            const controller = new AbortController();
            const timer = setTimeout(() => controller.abort(), 10000);
            fetch(url, {method:'GET', headers:{Authorization:'Bearer ' + token, Accept:'application/json'},
              signal:controller.signal})
              .then(response => done(response.status))
              .catch(() => done(-1))
              .finally(() => clearTimeout(timer));
            """, providerReadUrl));
        Assert.True(status == 403, $"A valid Visitor must receive HTTP 403 from the Provider-only bookings GET; received {status} (-1 means timeout/network failure).");
        AssertVisitorBoundary();
        browser.Capture("visitor-denied-provider-access");
        browser.Step("Visitor remained a Visitor after reopening the app; Provider navigation absent; real Provider-only bookings GET returned HTTP 403.");
    }

    private void AssertNoSession()
    {
        login.AssertUnauthenticated();
        Assert.True(browser.Script.ExecuteScript("return !localStorage.getItem('authToken') && !localStorage.getItem('userRole')") is true);
    }

    private void AssertVisitorBoundary()
    {
        browser.Visible(By.Id("nav-explore"));
        browser.Visible(By.Id("nav-profile"));
        Assert.Equal("Visitor", browser.Script.ExecuteScript("return localStorage.getItem('userRole')"));
        Assert.True(browser.Script.ExecuteScript("return Boolean(localStorage.getItem('authToken'))") is true);
        AssertNoProviderNavigation();
        Assert.Empty(browser.Driver.FindElements(By.CssSelector(".pd-overview, .pd-activities-tab, .pd-reviews-view, .pd-account-view")));
    }

    private void AssertNoProviderNavigation()
    {
        foreach (var key in ProviderOnlyNavigation)
            Assert.Empty(browser.Driver.FindElements(By.Id("nav-" + key)));
    }
}
