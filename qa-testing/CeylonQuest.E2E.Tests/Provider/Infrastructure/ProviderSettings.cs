using CeylonQuest.E2E.Tests.Infrastructure;
using Xunit;

namespace CeylonQuest.E2E.Tests.Provider.Infrastructure;

internal static class ProviderSettings
{
    // AdminSettings also references this constant. Keep that dependency unchanged.
    internal const string ProductionFrontend = "https://jolly-field-0aaea8a00.7.azurestaticapps.net";
    public static string Required(string name) => TestSettings.Required(name);
    public static string? ListingId => string.IsNullOrWhiteSpace(TestSettings.Optional("CQ_E2E_PROVIDER_LISTING_ID"))
        ? null : Guid.Parse(TestSettings.Required("CQ_E2E_PROVIDER_LISTING_ID")).ToString();
}

public sealed class ProviderFactAttribute : FactAttribute
{
    public ProviderFactAttribute()
    {
        var missing = new[] { "CQ_E2E_PROVIDER_EMAIL", "CQ_E2E_PROVIDER_PASSWORD" }
            .Where(name => string.IsNullOrWhiteSpace(TestSettings.Optional(name))).ToArray();
        if (missing.Length > 0) Skip = "Missing Provider configuration: " + string.Join(", ", missing);
    }
}

public sealed class ProviderTheoryAttribute : TheoryAttribute
{
    public ProviderTheoryAttribute()
    {
        var missing = new[] { "CQ_E2E_PROVIDER_EMAIL", "CQ_E2E_PROVIDER_PASSWORD" }
            .Where(name => string.IsNullOrWhiteSpace(TestSettings.Optional(name))).ToArray();
        if (missing.Length > 0) Skip = "Missing Provider configuration: " + string.Join(", ", missing);
    }
}
