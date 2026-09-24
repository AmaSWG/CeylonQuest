using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookingService.DTOs;

namespace BookingService.Services;

public class CatalogService : ICatalogService
{
    private readonly HttpClient _httpClient;

    public CatalogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CatalogAvailabilityResponse?>
        GetAvailabilityAsync(
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

    public async Task<CatalogListingResponse?>
        GetListingAsync(Guid listingId)
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

    public async Task<CatalogRestaurantResponse?>
        GetRestaurantAsync(Guid restaurantId)
    {
        var response = await _httpClient.GetAsync(
            "/api/catalog/restaurant-listings/public");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var restaurants =
            await response.Content
                .ReadFromJsonAsync<
                    List<CatalogRestaurantResponse>>();

        if (restaurants == null)
        {
            return null;
        }

        return restaurants.FirstOrDefault(
            restaurant => restaurant.Id == restaurantId);
    }

    public async Task<bool> ReserveCapacityAsync(
        Guid listingId,
        DateOnly date,
        string timeSlot,
        int participantCount)
    {
        var request = new
        {
            ListingId = listingId,
            Date = date.ToString("yyyy-MM-dd"),
            TimeSlot = timeSlot,
            GuestCount = participantCount
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/catalog/availability/reserve",
            request);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }

        return false;
    }

    // =====================================================
    // Story 9.1 - Provider-owned activity listings
    // =====================================================

    public async Task<List<CatalogProviderListingResponse>>
        GetMyActivityListingsAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/catalog/activity-listings");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response =
            await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return new List<CatalogProviderListingResponse>();
        }

        return await response.Content
                   .ReadFromJsonAsync<
                       List<CatalogProviderListingResponse>>()
               ?? new List<CatalogProviderListingResponse>();
    }

    // =====================================================
    // Story 9.1 - Provider-owned restaurant listings
    // =====================================================

    public async Task<List<CatalogProviderListingResponse>>
        GetMyRestaurantListingsAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/catalog/restaurant-listings");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response =
            await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return new List<CatalogProviderListingResponse>();
        }

        return await response.Content
                   .ReadFromJsonAsync<
                       List<CatalogProviderListingResponse>>()
               ?? new List<CatalogProviderListingResponse>();
    }
}