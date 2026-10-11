using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

public static class DriverFactory
{
    public static IWebDriver Create()
    {
        var options = new ChromeOptions();
        if (TestConfig.Headless) options.AddArgument("--headless=new");
        options.AddArgument("--window-size=1366,900");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        return new ChromeDriver(options);
    }

    public static WebDriverWait Wait(IWebDriver driver, TimeSpan? timeout = null) =>
        new(driver, timeout ?? TimeSpan.FromSeconds(15));
}
