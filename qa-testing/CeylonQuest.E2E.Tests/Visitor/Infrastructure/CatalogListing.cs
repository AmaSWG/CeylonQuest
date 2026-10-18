using System.Globalization;
using System.Text.Json;

namespace CeylonQuest.E2E.Tests.Visitor.Infrastructure;

internal sealed record CatalogListing(JsonElement Data)
{
    public string Text(string key, string fallback = "") => Data.TryGetProperty(key, out var value) &&
        value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : fallback;
    public int Int(string key) => Data.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number
        ? value.GetInt32() : 0;
    public decimal Number(string key) => Data.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number
        ? value.GetDecimal() : 0;
    public string Id => Text("id");
    public string Type => Text("type");
    public string Title => Text("title");
    public string Location => Text("location");
    public string Provider => Text("providerBusinessName");
    public decimal Price => Number("price");
    public string Unit => Text("unit") is { Length: > 0 } unit ? unit : Type == "Accommodation" ? "night" : "person";
    public DateTimeOffset CreatedAt => DateTimeOffset.Parse(Text("createdAt"), CultureInfo.InvariantCulture);
    public static readonly string[] SearchFields = ["title", "description", "location", "providerBusinessName"];
    public bool Matches(string term) => SearchFields.Any(field => Text(field).Contains(term, StringComparison.OrdinalIgnoreCase));

    public string[] Images
    {
        get
        {
            if (!Data.TryGetProperty("images", out var value) || value.ValueKind == JsonValueKind.Null) return [];
            if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray()
                .Select(image => image.GetString() ?? "").Where(image => !string.IsNullOrWhiteSpace(image)).ToArray();
            var raw = value.GetString() ?? "";
            if (raw.TrimStart().StartsWith('[')) return JsonSerializer.Deserialize<string[]>(raw) ?? [];
            return raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }
    }

    public static string PriceBand(decimal price) => price < 5000 ? "budget" : price <= 20000 ? "mid" : "luxury";
}
