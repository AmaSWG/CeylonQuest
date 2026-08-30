using OpenQA.Selenium;
using CeylonQuest.Tests.Configuration;

namespace CeylonQuest.Tests.Pages;

public class HomePage
{
    private readonly IWebDriver driver;

    public HomePage(IWebDriver driver)
    {
        this.driver = driver;
    }

    public void Open()
    {
        driver.Navigate().GoToUrl(TestConfiguration.BaseUrl);
    }

    public string GetTitle()
    {
        return driver.Title;
    }
}