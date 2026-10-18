using CeylonQuest.E2E.Tests.Infrastructure;
using OpenQA.Selenium;
using Xunit;

namespace CeylonQuest.E2E.Tests.Pages;

internal sealed class LoginPage(BrowserSession browser)
{
    public void OpenFromHome()
    {
        browser.Driver.Navigate().GoToUrl(TestSettings.BaseUrl);
        browser.Click(By.Id("nav-login-btn"));
        browser.Visible(By.Id("login-email"));
        browser.Visible(By.Id("login-password"));
        browser.Visible(By.Id("login-button"));
    }

    public void Submit(string email, string password)
    {
        browser.Fill(By.Id("login-email"), email);
        browser.Fill(By.Id("login-password"), password);
        browser.Click(By.Id("login-button"));
    }

    public void LoginVisitor()
    {
        OpenFromHome();
        Submit(TestSettings.Required("CQ_E2E_VISITOR_EMAIL"), TestSettings.Required("CQ_E2E_VISITOR_PASSWORD"));
        browser.Until(d => d.FindElements(By.Id("nav-explore")).Any(e => e.Displayed),
            "Visitor dashboard was not displayed after login. Check credentials, account role and Identity service.");
        Assert.Equal("Visitor", browser.Script.ExecuteScript("return localStorage.getItem('userRole')"));
        Assert.True(browser.Script.ExecuteScript("return Boolean(localStorage.getItem('authToken'))") is true);
        browser.Visible(By.Id("nav-profile"));
        browser.Step("Visitor authenticated and dashboard navigation displayed (navigation is React state, not a URL redirect).");
    }

    public string Error => browser.Visible(By.CssSelector(".login-error-banner[role='alert']")).Text;
    public void AssertUnauthenticated()
    {
        Assert.True(browser.Script.ExecuteScript("return !localStorage.getItem('authToken')") is true);
        Assert.Empty(browser.Driver.FindElements(By.Id("nav-explore")));
    }
}
