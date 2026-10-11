using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

/// <summary>
/// Drives the real Login page (frontend/src/features/auth/pages/Login.jsx) and the dashboard
/// shell both Visitor and Provider dashboards share (frontend/src/components/DashboardLayout.jsx),
/// which is where the notification bell/badge (TC-11.1-UI-01) live.
/// </summary>
public sealed class AppShell
{
    public IWebDriver Driver { get; }
    private WebDriverWait Wait => DriverFactory.Wait(Driver);

    public AppShell(IWebDriver driver) => Driver = driver;

    /// <summary>Clears any existing session and lands on the login page.</summary>
    public void StartFresh()
    {
        Driver.Navigate().GoToUrl(TestConfig.FrontendBaseUrl + "/?page=login");
        ((IJavaScriptExecutor)Driver).ExecuteScript("window.localStorage.clear(); window.sessionStorage.clear();");
        Driver.Navigate().GoToUrl(TestConfig.FrontendBaseUrl + "/?page=login");
        Wait.Until(d => d.FindElements(By.Id("login-email")).Count > 0);
    }

    /// <summary>Submits the real login form and waits for the dashboard shell (bell) to render.</summary>
    public void LoginAs(string email, string password)
    {
        StartFresh();
        Driver.FindElement(By.Id("login-email")).SendKeys(email);
        Driver.FindElement(By.Id("login-password")).SendKeys(password);
        Driver.FindElement(By.Id("login-button")).Click();
        Wait.Until(d => d.FindElements(By.CssSelector(".cq-notification-bell")).Count > 0);

        // Navigate to the app base URL so the "?page=login" query string is no longer in
        // the address bar; subsequent Refresh() calls will reload the dashboard, not the
        // login page.
        Driver.Navigate().GoToUrl(TestConfig.FrontendBaseUrl + "/");
        Wait.Until(d => d.FindElements(By.CssSelector(".cq-notification-bell")).Count > 0);
    }

    public bool IsBellVisible() => Driver.FindElements(By.CssSelector(".cq-notification-bell")).Count > 0;

    public int ReadBellBadgeCount()
    {
        var badges = Driver.FindElements(By.CssSelector(".cq-notification-bell__badge"));
        if (badges.Count == 0) return 0;
        var text = badges[0].Text.Trim();
        if (text.EndsWith('+')) return 100; // UI collapses >99 to "99+"
        return int.TryParse(text, out var count) ? count : 0;
    }

    public void OpenNotifications()
    {
        Driver.FindElement(By.CssSelector(".cq-notification-bell")).Click();
        Wait.Until(d => d.FindElements(By.CssSelector(".cq-notifications")).Count > 0);
    }

    public void OpenBookings()
    {
        Driver.FindElement(By.Id("nav-bookings")).Click();
        Wait.Until(d => d.FindElements(By.CssSelector(".vb-page")).Count > 0);
    }

    public void Refresh()
    {
        Driver.Navigate().Refresh();
        Wait.Until(d => d.FindElements(By.CssSelector(".cq-notification-bell")).Count > 0);
    }

    public string? ReadAuthToken() =>
        (string?)((IJavaScriptExecutor)Driver).ExecuteScript("return window.localStorage.getItem('authToken');");
}
