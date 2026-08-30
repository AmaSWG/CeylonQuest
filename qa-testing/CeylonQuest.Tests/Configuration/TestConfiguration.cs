using Microsoft.Extensions.Configuration;

namespace CeylonQuest.Tests.Configuration;

public static class TestConfiguration
{
    private static readonly IConfigurationRoot Configuration;

    static TestConfiguration()
    {
        string environment =
            System.Environment.GetEnvironmentVariable("TEST_ENVIRONMENT")
            ?? "Local"; // if TEST_ENVIRONMENT not specified it automatically use local

        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile(
                $"appsettings.{environment}.json",
                optional:false)
            .Build();
    }

    public static string BaseUrl =>
        Configuration["TestSettings:BaseUrl"]
        ?? throw new InvalidOperationException(
        "BaseUrl is not configured.");

    public static string Browser =>
        Configuration["TestSettings:Browser"]
        ?? "Chrome";

    public static string Environment =>
        Configuration["TestSettings:Environment"]
        ?? "Local";

    public static string VisitorEmail =>
        System.Environment.GetEnvironmentVariable("TEST_VISITOR_EMAIL")
        ?? throw new InvalidOperationException(
            "TEST_VISITOR_EMAIL environment variable is not configured.");

    public static string VisitorPassword =>
        System.Environment.GetEnvironmentVariable("TEST_VISITOR_PASSWORD")
        ?? throw new InvalidOperationException(
            "TEST_VISITOR_PASSWORD environment variable is not configured.");

    public static string ProviderDuplicateEmail =>
        System.Environment.GetEnvironmentVariable("TEST_PROVIDER_DUPLICATE_EMAIL")
        ?? throw new InvalidOperationException(
            "TEST_PROVIDER_DUPLICATE_EMAIL environment variable is not configured.");

    public static string AdminEmail =>
        System.Environment.GetEnvironmentVariable("TEST_ADMIN_EMAIL")
        ?? throw new InvalidOperationException(
            "TEST_ADMIN_EMAIL is not configured.");

    public static string AdminPassword =>
        System.Environment.GetEnvironmentVariable("TEST_ADMIN_PASSWORD")
        ?? throw new InvalidOperationException(
            "TEST_ADMIN_PASSWORD is not configured.");

    public static string ProviderEmail =>
        System.Environment.GetEnvironmentVariable("TEST_PROVIDER_EMAIL")
        ?? throw new InvalidOperationException(
            "TEST_PROVIDER_EMAIL is not configured."
        );

    public static string ProviderPassword =>
        System.Environment.GetEnvironmentVariable("TEST_PROVIDER_PASSWORD")
        ?? throw new InvalidOperationException(
            "TEST_PROVIDER_PASSWORD is not configured."
        );

    public static string PendingProviderEmail =>
        System.Environment.GetEnvironmentVariable(
            "TEST_PENDING_PROVIDER_EMAIL"
        )
        ?? throw new InvalidOperationException(
            "TEST_PENDING_PROVIDER_EMAIL is not configured."
        );

    public static string ApprovedProviderEmail =>
        System.Environment.GetEnvironmentVariable(
            "TEST_APPROVED_PROVIDER_EMAIL"
        )
        ?? throw new InvalidOperationException(
            "TEST_APPROVED_PROVIDER_EMAIL is not configured."
        );

}