using System.Net;
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

    // Story 7.1 / Story 8.1
    // Get availability for a listing on a selected date
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

    // Story 7.1
    // Get experience/listing information
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

    // Story 8.1
    // Get restaurant information from the public restaurant endpoint
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

    // Story 7.1 / Story 8.1
    // Reserve capacity before creating booking/reservation
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

        // 409 means requested capacity could not be reserved
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }

        return false;
    }
}