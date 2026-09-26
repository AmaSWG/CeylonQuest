using BookingService.DTOs;

namespace BookingService.Services;

public interface ICatalogService
{
    Task<CatalogAvailabilityResponse?> GetAvailabilityAsync(
        Guid listingId,
        DateOnly date);

    Task<CatalogListingResponse?> GetListingAsync(
        Guid listingId);

    Task<CatalogRestaurantResponse?> GetRestaurantAsync(
        Guid restaurantId);

    Task<CatalogAccommodationResponse?> GetAccommodationAsync(
        Guid accommodationId);

    Task<bool> ReserveCapacityAsync(
        Guid listingId,
        DateOnly date,
        string timeSlot,
        int participantCount);

    // Story 9.1 - Provider booking management
    Task<List<CatalogProviderListingResponse>>
        GetMyActivityListingsAsync(string accessToken);

    Task<List<CatalogProviderListingResponse>>
        GetMyRestaurantListingsAsync(string accessToken);
}