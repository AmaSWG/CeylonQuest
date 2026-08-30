using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages;

public class RoleAccessPage
{
    private readonly IWebDriver driver;
    private readonly WebDriverWait wait;

    public RoleAccessPage(IWebDriver driver)
    {
        this.driver = driver;
        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public bool IsVisitorDashboardDisplayed()
    {
        try
        {
            return wait.Until(d =>
                d.FindElement(By.CssSelector(".vd-page")).Displayed);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public bool IsAdminDashboardDisplayed()
    {
        try
        {
            return wait.Until(d =>
                d.FindElement(By.CssSelector(".ad-page")).Displayed);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public bool IsAdminUserManagementAvailable()
    {
        return driver.FindElements(By.Id("ad-nav-users"))
            .Any(e => e.Displayed);
    }

    public bool IsAdminProviderManagementAvailable()
    {
        return driver.FindElements(By.Id("ad-nav-providers"))
            .Any(e => e.Displayed);
    }

    public bool IsAdminReportsAvailable()
    {
        return driver.FindElements(By.Id("ad-nav-reports"))
            .Any(e => e.Displayed);
    }

    public bool IsVisitorDashboardPresent()
    {
        return driver
            .FindElements(By.CssSelector(".vd-page"))
            .Any(e => e.Displayed);
    }

    //logout helpers
    public void ClickVisitorLogout()
    {
        IWebElement logoutButton = wait.Until(d =>
            d.FindElement(By.Id("logout-btn"))
        );

        logoutButton.Click();
    }

    public void ClickAdminLogout()
    {
        IWebElement logoutButton = wait.Until(d =>
            d.FindElement(By.Id("ad-logout-btn"))
        );

        logoutButton.Click();
    }

    public bool IsAdminDashboardPresent()
    {
        return driver
            .FindElements(By.CssSelector(".ad-page"))
            .Any(e => e.Displayed);
    }

    public bool IsProviderDashboardDisplayed()
    {
        try
        {
            return wait.Until(d =>
                d.FindElement(By.CssSelector(".pd-page")).Displayed
            );
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

}