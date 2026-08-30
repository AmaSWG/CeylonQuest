// Create the browser
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using CeylonQuest.Tests.Configuration;

namespace CeylonQuest.Tests.Utilities;

public static class DriverFactory
{
    public static IWebDriver CreateDriver()
    {
        string browser = TestConfiguration.Browser;

        switch (browser.ToLower())
        {
            case "chrome":
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--start-maximized");

                return new ChromeDriver(options);

            default:
                throw new ArgumentException(
                    $"Browser '{browser}' is not supported.");
        }
    }
}