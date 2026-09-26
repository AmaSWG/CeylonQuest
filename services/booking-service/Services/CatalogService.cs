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

    // =========================================================
    // Experience / Restaurant / Accommodation Availability
    // =========================================================

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

    // =========================================================
    // Experience
    // =========================================================

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

    // =========================================================
    // Restaurant
    // =========================================================

    public async Task<CatalogRestaurantResponse?> GetRestaurantAsync(
        Guid restaurantId)
    {
        var response = await _httpClient.GetAsync(
            "/api/catalog/restaurant-listings/public");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var restaurants =
            await response.Content
                .ReadFromJsonAsync<List<CatalogRestaurantResponse>>();

        if (restaurants == null)
        {
            return null;
        }

        return restaurants.FirstOrDefault(
            restaurant => restaurant.Id == restaurantId);
    }

    // =========================================================
    // Accommodation
    // =========================================================

    public async Task<CatalogAccommodationResponse?> GetAccommodationAsync(
        Guid accommodationId)
    {
        var response = await _httpClient.GetAsync(
            "/api/catalog/accommodation-listings/public");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var accommodations =
            await response.Content
                .ReadFromJsonAsync<List<CatalogAccommodationResponse>>();

        if (accommodations == null)
        {
            return null;
        }

        return accommodations.FirstOrDefault(
            accommodation => accommodation.Id == accommodationId);
    }

    // =========================================================
    // Reserve Capacity
    // =========================================================

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

    // =========================================================
    // Provider-owned Activity Listings
    // =========================================================

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
                   .ReadFromJsonAsync<List<CatalogProviderListingResponse>>()
               ?? new List<CatalogProviderListingResponse>();
    }

    // =========================================================
    // Provider-owned Restaurant Listings
    // =========================================================

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
                   .ReadFromJsonAsync<List<CatalogProviderListingResponse>>()
               ?? new List<CatalogProviderListingResponse>();
    }

    // =========================================================
    // Provider-owned Accommodation Listings
    // =========================================================

    public async Task<List<CatalogProviderListingResponse>>
        GetMyAccommodationListingsAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/catalog/accommodation-listings");

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
                   .ReadFromJsonAsync<List<CatalogProviderListingResponse>>()
               ?? new List<CatalogProviderListingResponse>();
    }
}