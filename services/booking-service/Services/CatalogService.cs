using System.Net.Http.Json;
using BookingService.DTOs;

namespace BookingService.Services;

public class CatalogService
{
    private readonly HttpClient _httpClient;

    public CatalogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CatalogAvailabilityResponse?> GetAvailabilityAsync(
        Guid listingId,
        DateOnly date)
    {
        var url =
            $"/api/catalog/availability/{listingId}?date={date:yyyy-MM-dd}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<CatalogAvailabilityResponse>();
    }

    public async Task<CatalogListingResponse?> GetListingAsync(
    Guid listingId)
{
    var url =
        $"/api/catalog/activity-listings/public/{listingId}";

    var response = await _httpClient.GetAsync(url);

    if (!response.IsSuccessStatusCode)
    {
        return null;
    }

    return await response.Content
        .ReadFromJsonAsync<CatalogListingResponse>();
}

}