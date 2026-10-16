using CeylonQuest.E2E.Tests.Infrastructure;
using CeylonQuest.E2E.Tests.Pages;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Tests;

public sealed class AuthenticationTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "Smoke")]
    public void Home_LoginNavigation_DisplaysLoginForm()
    {
        using var browser = new BrowserSession(nameof(Home_LoginNavigation_DisplaysLoginForm), output);
        browser.Run(() => {
            new LoginPage(browser).OpenFromHome();
            Assert.Equal("email", browser.Visible(By.Id("login-email")).GetDomAttribute("type"));
            Assert.Equal("password", browser.Visible(By.Id("login-password")).GetDomAttribute("type"));
            browser.Capture("login-form");
        });
    }

    [VisitorFact]
    [Trait("Category", "Authentication")]
    public void ValidVisitorLogin_DisplaysVisitorDashboard()
    {
        using var browser = new BrowserSession(nameof(ValidVisitorLogin_DisplaysVisitorDashboard), output);
        browser.Run(() => {
            new LoginPage(browser).LoginVisitor();
            browser.Capture("visitor-dashboard");
        });
    }

    [Fact]
    [Trait("Category", "Validation")]
    public void InvalidCredentials_DisplayAuthenticationError()
    {
        using var browser = new BrowserSession(nameof(InvalidCredentials_DisplayAuthenticationError), output);
        browser.Run(() => {
            var login = new LoginPage(browser);
            login.OpenFromHome();
            // A reserved, unique invalid address cannot lock out a real visitor account.
            login.Submit($"selenium-{Guid.NewGuid():N}@example.invalid", Guid.NewGuid().ToString("N"));
            Assert.Contains("invalid", login.Error, StringComparison.OrdinalIgnoreCase);
            login.AssertUnauthenticated();
            browser.Capture("invalid-login");
        });
    }

    [Fact]
    [Trait("Category", "Validation")]
    public void EmptyLoginFields_DisplayValidationError()
    {
        using var browser = new BrowserSession(nameof(EmptyLoginFields_DisplayValidationError), output);
        browser.Run(() => {
            var login = new LoginPage(browser);
            login.OpenFromHome();
            // The real form uses noValidate and relies on the Identity API's 400 response.
            browser.Click(By.Id("login-button"));
            Assert.Equal("Please check your login details.", login.Error);
            login.AssertUnauthenticated();
            browser.Capture("empty-login");
        });
    }
}
