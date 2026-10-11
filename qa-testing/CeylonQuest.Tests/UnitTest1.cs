 using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace CeylonQuest.Tests;

public class BrowserTest
{
    [Fact]
    public void OpenCeylonQuest()
    {
        IWebDriver driver = new ChromeDriver();

        driver.Navigate().GoToUrl("http://localhost:5173");

        Assert.NotNull(driver.Title);

        driver.Quit();
    }
}
