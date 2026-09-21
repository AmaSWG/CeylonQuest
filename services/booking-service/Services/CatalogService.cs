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

    // Get availability for an experience on a selected date
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

    // Reserve capacity in Provider Catalog before creating the booking
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

        // 409 means the requested capacity could not be reserved.
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }

        return false;
    }
}