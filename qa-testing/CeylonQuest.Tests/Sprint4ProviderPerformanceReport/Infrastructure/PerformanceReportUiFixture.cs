using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace CeylonQuest.Tests.Sprint4ProviderPerformanceReport.Infrastructure;

public sealed class PerformanceReportUiFixture : IDisposable
{
    public IWebDriver Driver { get; }
    public PerformanceReportApiClient Api { get; }

    public PerformanceReportUiFixture()
    {
        var opts = new ChromeOptions();
        if (PerformanceReportTestConfig.Headless)
        {
            opts.AddArgument("--headless=new");
        }
        opts.AddArgument("--window-size=1600,1000");
        opts.AddArgument("--no-sandbox");
        opts.AddArgument("--disable-dev-shm-usage");
        opts.AddArgument("--disable-gpu");

        Driver = new ChromeDriver(opts);
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
        Api = new PerformanceReportApiClient();
    }

    public void Dispose()
    {
        try
        {
            Driver.Quit();
            Driver.Dispose();
        }
        catch
        {
            // Ignore teardown errors
        }
    }
}

[CollectionDefinition(nameof(PerformanceReportUiCollection))]
public class PerformanceReportUiCollection : ICollectionFixture<PerformanceReportUiFixture>
{
}
