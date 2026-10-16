using System.Globalization;

namespace CeylonQuest.E2E.Tests.Infrastructure;

internal static class TestSettings
{
    public static string? Optional(string name) => Environment.GetEnvironmentVariable(name);
    public static string Required(string name) => !string.IsNullOrWhiteSpace(Optional(name))
        ? Optional(name)! : throw new InvalidOperationException($"Set {name} before running this test.");
    public static string BaseUrl => (Optional("CQ_E2E_BASE_URL") ??
        "https://jolly-field-0aaea8a00.7.azurestaticapps.net").TrimEnd('/');
    public static bool Headless => !string.Equals(Optional("CQ_E2E_HEADLESS"), "false", StringComparison.OrdinalIgnoreCase);
    public static bool Enabled(string name) => string.Equals(Optional(name), "true", StringComparison.OrdinalIgnoreCase);
    public static int Seconds(string name, int fallback) => Optional(name) is { Length: > 0 } value
        ? int.Parse(value, CultureInfo.InvariantCulture) is > 0 and var seconds
            ? seconds : throw new InvalidOperationException($"{name} must be positive.")
        : fallback;
    public static int Guests => Optional("CQ_E2E_GUESTS") is { Length: > 0 } value
        ? int.Parse(value, CultureInfo.InvariantCulture) : 1;
    public static DateOnly Date(string name) => DateOnly.ParseExact(Required(name), "yyyy-MM-dd", CultureInfo.InvariantCulture);
    public static DateOnly TodayInSriLanka => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
        DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo")));
    public static void ValidateStay()
    {
        if (Date("CQ_E2E_CHECK_IN") <= TodayInSriLanka || Date("CQ_E2E_CHECK_OUT") <= Date("CQ_E2E_CHECK_IN"))
            throw new InvalidOperationException("Use a future Sri Lanka check-in date and a later check-out date.");
        if (Guests < 1) throw new InvalidOperationException("CQ_E2E_GUESTS must be at least 1.");
    }
}
