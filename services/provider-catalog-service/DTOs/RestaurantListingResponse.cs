using System;

namespace ProviderCatalogService.DTOs;

public class RestaurantListingResponse
{
    public Guid Id { get; set; }

    public Guid ProviderId { get; set; }

    public string ProviderBusinessName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CuisineType { get; set; } = string.Empty;

    public string DiningStyle { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public decimal PricePerPerson { get; set; }

    public string PriceRange { get; set; } = string.Empty;

    public string OpeningHours { get; set; } = string.Empty;

    public string SetMenuDetails { get; set; } = string.Empty;

    public string DietaryOptions { get; set; } = string.Empty;

    public string GroupSizeCategory { get; set; } = string.Empty;

    public int SeatingCapacity { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}