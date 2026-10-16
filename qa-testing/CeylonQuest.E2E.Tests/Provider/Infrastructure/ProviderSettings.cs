using CeylonQuest.E2E.Tests.Infrastructure;
using Xunit;

namespace CeylonQuest.E2E.Tests.Provider.Infrastructure;

internal static class ProviderSettings
{
    internal const string ProductionFrontend = "https://jolly-field-0aaea8a00.7.azurestaticapps.net";
    private static readonly string[] ProductionApis = [
        "ceylonquest-identity-fphphwe7cfbtadhu.southeastasia-01.azurewebsites.net",
        "ceylonquest-provider-catalog-fecca6csaubwa2em.southeastasia-01.azurewebsites.net",
        "ceylonquest-booking-service-gccuc7fbhyckcpg9.southeastasia-01.azurewebsites.net"
    ];

    public static string Required(string name) => TestSettings.Required(name);
    public static string Id(string name) => Guid.Parse(Required(name)).ToString();

    public static string? WriteBlockReason()
    {
        if (!TestSettings.Enabled("CQ_E2E_PROVIDER_ALLOW_WRITES"))
            return "Provider writes disabled. Set CQ_E2E_PROVIDER_ALLOW_WRITES=true only in an isolated test deployment.";
        if (!TestSettings.Enabled("CQ_E2E_PROVIDER_TEST_ENVIRONMENT_CONFIRMED") ||
            !TestSettings.Enabled("CQ_E2E_PROVIDER_TEST_ACCOUNT_CONFIRMED"))
            return "Provider writes require a confirmed isolated test environment AND dedicated test account.";
        if (!Uri.TryCreate(TestSettings.BaseUrl, UriKind.Absolute, out var target) ||
            target.Host.Equals(new Uri(ProductionFrontend).Host, StringComparison.OrdinalIgnoreCase))
            return "Provider writes are blocked on the shared deployed website. Supply an isolated CQ_E2E_BASE_URL.";
        if (string.IsNullOrWhiteSpace(TestSettings.Optional("CQ_E2E_PROVIDER_TEST_API_ORIGINS")))
            return "Set CQ_E2E_PROVIDER_TEST_API_ORIGINS to the isolated backend origins before enabling writes.";
        return null;
    }

    public static void AssertWriteTarget(string observedUiUrl)
    {
        Assert.Null(WriteBlockReason());
        var target = new Uri(observedUiUrl);
        Assert.DoesNotContain(target.Host, ProductionApis, StringComparer.OrdinalIgnoreCase);
        var allowed = Required("CQ_E2E_PROVIDER_TEST_API_ORIGINS").Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => new Uri(value.Trim()).GetLeftPart(UriPartial.Authority)).ToArray();
        Assert.Contains(target.GetLeftPart(UriPartial.Authority), allowed, StringComparer.OrdinalIgnoreCase);
    }
}

// These attributes are independent of VisitorFact, so Provider credentials do not
// cause existing Visitor tests to run or change their discovery behavior.
public class ProviderFactAttribute : FactAttribute
{
    public ProviderFactAttribute(params string[] required)
    {
        var missing = new[] { "CQ_E2E_PROVIDER_EMAIL", "CQ_E2E_PROVIDER_PASSWORD" }.Concat(required)
            .Where(name => string.IsNullOrWhiteSpace(TestSettings.Optional(name))).ToArray();
        if (missing.Length > 0) Skip = "Missing Provider configuration: " + string.Join(", ", missing);
    }
}

public sealed class ProviderWriteFactAttribute : ProviderFactAttribute
{
    public ProviderWriteFactAttribute(params string[] required) : base(required)
    {
        if (Skip is null) Skip = ProviderSettings.WriteBlockReason();
    }
}
