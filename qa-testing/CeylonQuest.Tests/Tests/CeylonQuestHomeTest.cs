using CeylonQuest.Tests.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class CeylonQuestHomeTest
{
    [Fact]
    public void OpenCeylonQuestHomePage()
    {
        var config = TestConfiguration.Load();
        var baseUrl = config["TestSettings:BaseUrl"]
    ?? throw new InvalidOperationException("TestSettings:BaseUrl is not configured.");

var driverPath = Environment.GetEnvironmentVariable("CHROMEDRIVER_PATH");

using var driver = string.IsNullOrWhiteSpace(driverPath)
    ? new ChromeDriver()
    : new ChromeDriver(driverPath);

        driver.Navigate().GoToUrl(baseUrl);

        Assert.Contains("CeylonQuest", driver.PageSource);
    }
}