// Browser lifecycle
using OpenQA.Selenium;
using CeylonQuest.Tests.Utilities;

namespace CeylonQuest.Tests.Tests;

public abstract class BaseTest : IDisposable
{
    protected IWebDriver Driver { get; }

    protected BaseTest() // help to create the chrome
    {
        Driver = DriverFactory.CreateDriver();
    }

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose(); // close the chrome
    }
}