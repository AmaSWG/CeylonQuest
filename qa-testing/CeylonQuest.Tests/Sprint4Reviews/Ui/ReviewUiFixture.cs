using CeylonQuest.Tests.Sprint4Notifications.Infrastructure;
using CeylonQuest.Tests.Sprint4Reviews.Infrastructure;
using OpenQA.Selenium;

namespace CeylonQuest.Tests.Sprint4Reviews.Ui;

/// <summary>
/// Shared, expensive resources only (browser, HTTP clients). Each test method provisions its own
/// fresh visitor + booking data via the helpers on <see cref="ExperienceReviewUiTests"/> so
/// assertions are never polluted by another test's data.
/// </summary>
public sealed class ReviewUiFixture : IAsyncLifetime
{
    public IdentityApiClient Identity { get; } = new();
    public CatalogApiClient Catalog { get; } = new();
    public BookingApiClient Booking { get; } = new();
    public ReviewApiClient Reviews { get; } = new();
    public NotificationApiClient Notifications { get; } = new();
    public IWebDriver Driver { get; private set; } = null!;
    public AppShell Shell { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Driver = DriverFactory.Create();
        Shell = new AppShell(Driver);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Driver.Quit();
        Driver.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition(nameof(ReviewUiCollection), DisableParallelization = true)]
public sealed class ReviewUiCollection : ICollectionFixture<ReviewUiFixture>
{
}
