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

    public DateTime CreatedAt { get; set; }
}