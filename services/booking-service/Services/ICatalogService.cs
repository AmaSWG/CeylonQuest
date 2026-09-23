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

    Task<bool> ReserveCapacityAsync(
        Guid listingId,
        DateOnly date,
        string timeSlot,
        int participantCount);
}