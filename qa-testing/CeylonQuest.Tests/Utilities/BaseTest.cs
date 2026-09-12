using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace CeylonQuest.Tests.Utilities;

public class BaseTest : IDisposable
{
    protected IWebDriver Driver { get; }
    protected string BaseUrl { get; } = "http://localhost:5173";

    public BaseTest()
    {
        var options = new ChromeOptions();
        options.AddArgument("--start-maximized");

        Driver = new ChromeDriver(options);
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
        Driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(20);
    }

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }
}
