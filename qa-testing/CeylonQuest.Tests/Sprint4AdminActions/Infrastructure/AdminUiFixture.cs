using CeylonQuest.Tests.Sprint4Notifications.Infrastructure;
using OpenQA.Selenium;

namespace CeylonQuest.Tests.Sprint4AdminActions.Infrastructure;

public sealed class AdminUiFixture : IAsyncLifetime
{
    public IWebDriver Driver { get; private set; } = null!;
    public AppShell Shell { get; private set; } = null!;
    public AdminApiClient Api { get; } = new();
    public AdminTestDataHelper Data { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Driver = DriverFactory.Create();
        Shell = new AppShell(Driver);
        Data = new AdminTestDataHelper(Api);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        try
        {
            Driver.Quit();
            Driver.Dispose();
        }
        catch { }
        return Task.CompletedTask;
    }
}

[CollectionDefinition(nameof(AdminUiCollection), DisableParallelization = true)]
public sealed class AdminUiCollection : ICollectionFixture<AdminUiFixture>
{
}
