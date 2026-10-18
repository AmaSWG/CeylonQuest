using System.Globalization;
using CeylonQuest.E2E.Tests.Infrastructure;
using Xunit;

namespace CeylonQuest.E2E.Tests.Visitor.Infrastructure;

internal static class VisitorCatalogSettings
{
    public static string Prefix(string type) => "CQ_E2E_CATALOG_" + type.ToUpperInvariant();
    public static string? Title(string type) => TestSettings.Optional(Prefix(type) + "_TITLE");
    public static DateOnly Date(string type) => TestSettings.Date(Prefix(type) + "_DATE");
    public static int Guests => int.TryParse(TestSettings.Optional("CQ_E2E_CATALOG_GUESTS") ?? "1",
        NumberStyles.None, CultureInfo.InvariantCulture, out var count) && count > 0
        ? count : throw new InvalidOperationException("CQ_E2E_CATALOG_GUESTS must be a positive integer.");

    public static void ValidateDate(string type)
    {
        if (Date(type) <= TestSettings.TodayInSriLanka)
            throw new InvalidOperationException(Prefix(type) + "_DATE must be a future Sri Lanka date.");
        if (type == "Accommodation" && TestSettings.Date("CQ_E2E_CATALOG_CHECK_OUT") <= Date(type))
            throw new InvalidOperationException("CQ_E2E_CATALOG_CHECK_OUT must be after the accommodation date.");
    }
}

// These attributes apply only to the new suite. Existing authentication tests are untouched.
public sealed class CatalogFactAttribute : VisitorFactAttribute { }

public sealed class CatalogTheoryAttribute : TheoryAttribute
{
    public CatalogTheoryAttribute()
    {
        var missing = new[] { "CQ_E2E_VISITOR_EMAIL", "CQ_E2E_VISITOR_PASSWORD" }
            .Where(name => string.IsNullOrWhiteSpace(TestSettings.Optional(name))).ToArray();
        if (missing.Length > 0) Skip = "Missing environment variables: " + string.Join(", ", missing);
    }
}

public sealed class CatalogAvailabilityFactAttribute : VisitorFactAttribute
{
    public CatalogAvailabilityFactAttribute(string type)
    {
        if (Skip is not null) return;
        Require(VisitorCatalogSettings.Prefix(type) + "_TITLE", VisitorCatalogSettings.Prefix(type) + "_DATE");
        if (Skip is null && type == "Accommodation") Require("CQ_E2E_CATALOG_CHECK_OUT");
    }
}
