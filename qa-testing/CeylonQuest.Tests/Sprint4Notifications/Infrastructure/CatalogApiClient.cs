using System.Net.Http.Json;

namespace CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

/// <summary>Read-only helper used only by the optional, environment-dependent "real flow" bonus tests.</summary>
public sealed class CatalogApiClient
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(TestConfig.CatalogApiBaseUrl) };

    public async Task<CatalogSearchItem?> FindBookableListingAsync(string type, CancellationToken ct = default)
    {
        var items = await FindBookableListingsAsync(type, ct);
        return items.FirstOrDefault();
    }

    public async Task<List<CatalogSearchItem>> FindBookableListingsAsync(string type, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/catalog/search?type={type}&pageSize=50", ct);
        if (!response.IsSuccessStatusCode) return new();
        var payload = await response.Content.ReadFromJsonAsync<CatalogSearchResponse>(JsonDefaults.Options, ct);
        return payload?.Items ?? new();
    }

    public async Task<(string Date, string TimeSlot)?> FindAvailableSlotAsync(Guid listingId, CancellationToken ct = default)
    {
        for (var offset = 1; offset <= 35; offset++)
        {
            var date = DateTime.UtcNow.Date.AddDays(offset).ToString("yyyy-MM-dd");
            var response = await _http.GetAsync($"/api/catalog/availability/{listingId}?date={date}", ct);
            if (!response.IsSuccessStatusCode) continue;
            var availability = await response.Content.ReadFromJsonAsync<AvailabilityResponse>(JsonDefaults.Options, ct);
            var slot = availability?.Slots?.FirstOrDefault(s => s.RemainingCapacity > 0);
            if (slot != null) return (date, slot.TimeSlot);
        }
        return null;
    }

    private sealed record AvailabilityResponse(List<AvailabilitySlot>? Slots);
    private sealed record AvailabilitySlot(string TimeSlot, int RemainingCapacity);
}
