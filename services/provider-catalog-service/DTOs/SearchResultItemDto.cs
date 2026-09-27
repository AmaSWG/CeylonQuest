using System;

namespace ProviderCatalogService.DTOs;

public class SearchResultItemDto
{
    public Guid Id { get; set; }

    public string Type { get; set; } = "Experience"; // "Experience" | "Restaurant" | "Accommodation"

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string PriceFormatted { get; set; } = string.Empty;

    public string KeyDetail { get; set; } = string.Empty; // e.g. "Cuisine: Seafood" | "Duration: 2 Hours" | "Room: Deluxe Suite"

    public string ScheduleInfo { get; set; } = string.Empty; // e.g. "08:00 AM - 10:00 AM" | "11:30 AM - 10:00 PM" | "Min 1 Night"

    public string ProviderBusinessName { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    // Experience / Activity specific fields
    public string? Duration { get; set; }
    public int? MaxParticipants { get; set; }
    public string? AvailableDays { get; set; }
    public string? TimeSlots { get; set; }

    // Restaurant specific fields
    public string? CuisineType { get; set; }
    public string? DiningStyle { get; set; }
    public string? OpeningHours { get; set; }
    public int? SeatingCapacity { get; set; }
    public string? SetMenuDetails { get; set; }
    public string? DietaryOptions { get; set; }

    // Accommodation specific fields
    public string? PropertyType { get; set; }
    public int? MaxGuests { get; set; }
    public string? BedDetails { get; set; }
    public int? MinStayNights { get; set; }
    public string? Amenities { get; set; }
    public string? BathroomDetails { get; set; }

    public DateTime CreatedAt { get; set; }
    public List<string> Images { get; set; } = new();
}