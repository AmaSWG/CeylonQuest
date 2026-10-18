using CeylonQuest.E2E.Tests.Infrastructure;
using Xunit;

namespace CeylonQuest.E2E.Tests.Admin.Infrastructure;

internal static class AdminSettings
{
    public static string? OptionalId(string name) => string.IsNullOrWhiteSpace(TestSettings.Optional(name))
        ? null : Guid.Parse(TestSettings.Required(name)).ToString();
    public static int DocumentIndex => int.TryParse(TestSettings.Optional("CQ_E2E_ADMIN_CATALOG_DOCUMENT_INDEX") ?? "0", out var value) && value >= 0
        ? value : throw new InvalidOperationException("CQ_E2E_ADMIN_CATALOG_DOCUMENT_INDEX must be a nonnegative integer.");
}

public sealed class AdminFactAttribute : FactAttribute
{
    public AdminFactAttribute()
    {
        var missing = new[] { "CQ_E2E_ADMIN_EMAIL", "CQ_E2E_ADMIN_PASSWORD" }
            .Where(name => string.IsNullOrWhiteSpace(TestSettings.Optional(name))).ToArray();
        if (missing.Length > 0) Skip = "Missing Admin configuration: " + string.Join(", ", missing);
    }
}

public sealed class AdminTheoryAttribute : TheoryAttribute
{
    public AdminTheoryAttribute()
    {
        var missing = new[] { "CQ_E2E_ADMIN_EMAIL", "CQ_E2E_ADMIN_PASSWORD" }
            .Where(name => string.IsNullOrWhiteSpace(TestSettings.Optional(name))).ToArray();
        if (missing.Length > 0) Skip = "Missing Admin configuration: " + string.Join(", ", missing);
    }
}
