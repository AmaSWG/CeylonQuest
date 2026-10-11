using CeylonQuest.Tests.Sprint4Notifications.Infrastructure;
using OpenQA.Selenium;

namespace CeylonQuest.Tests.Sprint4Notifications.Ui;

/// <summary>
/// Shared, expensive resources only (browser, HTTP clients, Kafka producer). Each test method
/// provisions its own fresh users via <see cref="NewUserAsync"/> so absolute assertions
/// ("badge is 0", "exactly 3 unread") are never polluted by another test's data.
/// </summary>
public sealed class NotificationUiFixture : IAsyncLifetime
{
    public IdentityApiClient Identity { get; } = new();
    public NotificationApiClient NotificationsApi { get; } = new();
    public KafkaEventPublisher Kafka { get; private set; } = null!;
    public IWebDriver Driver { get; private set; } = null!;
    public AppShell Shell { get; private set; } = null!;

    public Task<TestUser> NewUserAsync(string label) => Identity.CreateVisitorAsync(label);

    public Task InitializeAsync()
    {
        Kafka = new KafkaEventPublisher();
        Driver = DriverFactory.Create();
        Shell = new AppShell(Driver);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Driver.Quit();
        Driver.Dispose();
        await Kafka.DisposeAsync();
    }
}

[CollectionDefinition(nameof(NotificationUiCollection), DisableParallelization = true)]
public sealed class NotificationUiCollection : ICollectionFixture<NotificationUiFixture>
{
}
