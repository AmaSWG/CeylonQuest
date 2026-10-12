using CeylonQuest.Tests.Sprint4Notifications;

namespace CeylonQuest.Tests.Sprint4ProviderPerformanceReport.Infrastructure;

public static class PerformanceReportTestConfig
{
    public static string FrontendBaseUrl => TestConfig.FrontendBaseUrl;
    public static string IdentityApiBaseUrl => TestConfig.IdentityApiBaseUrl;
    public static string BookingApiBaseUrl => TestConfig.BookingApiBaseUrl;
    public static string CatalogApiBaseUrl => TestConfig.CatalogApiBaseUrl;

    public static string AdminEmail => Env("CQ_ADMIN_EMAIL", "adminceylonquest@gmail.com");
    public static string AdminPassword => Env("CQ_ADMIN_PASSWORD", "AdminPassword123!");

    public static string ProviderEmail => TestConfig.ProviderEmail;
    public static string ProviderPassword => TestConfig.ProviderPassword;

    public static bool Headless => TestConfig.Headless;
    public static TimeSpan EventualConsistencyTimeout => TimeSpan.FromSeconds(15);
    public static TimeSpan PollInterval => TimeSpan.FromMilliseconds(500);

    private static string Env(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;
}
