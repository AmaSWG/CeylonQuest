using System;

namespace ProviderCatalogService.Models;

public class RestaurantListing
{
    public Guid Id { get; set; }

    public Guid ProviderId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CuisineType { get; set; } = string.Empty;

    public string DiningStyle { get; set; } = "Casual Dining";

    public string Location { get; set; } = string.Empty;

    public decimal PricePerPerson { get; set; }

    public string PriceRange { get; set; } = "$$ (Moderate)";

    public string OpeningHours { get; set; } = string.Empty;

    public string SetMenuDetails { get; set; } = string.Empty;

    public string DietaryOptions { get; set; } = "Standard";

    public string GroupSizeCategory { get; set; } = "Table for Two";

    public int SeatingCapacity { get; set; } = 20;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Provider? Provider { get; set; }
}