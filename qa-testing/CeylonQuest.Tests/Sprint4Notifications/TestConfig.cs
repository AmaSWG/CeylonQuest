namespace CeylonQuest.Tests.Sprint4Notifications;

/// <summary>
/// Central configuration for the Sprint 4 / Story 11.1 (notification-service) QA suite.
/// Every value can be overridden with an environment variable so the suite runs against
/// whichever hosts/ports the tester has the services on; the defaults match the local-dev
/// ports in docker-compose.yml / each service's Properties/launchSettings.json.
/// </summary>
public static class TestConfig
{
    public static string FrontendBaseUrl => Env("CQ_FRONTEND_URL", "http://localhost:5173");
    public static string IdentityApiBaseUrl => Env("CQ_IDENTITY_API_URL", "http://localhost:5278");
    public static string BookingApiBaseUrl => Env("CQ_BOOKING_API_URL", "http://localhost:5229");
    public static string CatalogApiBaseUrl => Env("CQ_CATALOG_API_URL", "http://localhost:5141");
    public static string NotificationApiBaseUrl => Env("CQ_NOTIFICATION_API_URL", "http://localhost:5185");
    public static string KafkaBootstrapServers => Env("CQ_KAFKA_BOOTSTRAP", "localhost:9092");

    /// <summary>Set CQ_HEADLESS=false to watch the browser while debugging a failing scenario.</summary>
    public static bool Headless =>
        !string.Equals(Environment.GetEnvironmentVariable("CQ_HEADLESS"), "false", StringComparison.OrdinalIgnoreCase);

    public static TimeSpan EventualConsistencyTimeout => TimeSpan.FromSeconds(20);
    public static TimeSpan PollInterval => TimeSpan.FromMilliseconds(500);

    private static string Env(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;
}
