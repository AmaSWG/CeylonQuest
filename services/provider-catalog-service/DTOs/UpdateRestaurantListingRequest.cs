using System.ComponentModel.DataAnnotations;

namespace ProviderCatalogService.DTOs;

public class UpdateRestaurantListingRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string CuisineType { get; set; } = string.Empty;

    [Required]
    public string DiningStyle { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    [Range(
        0.01,
        double.MaxValue,
        ErrorMessage = "Price per person must be greater than 0."
    )]
    public decimal PricePerPerson { get; set; }

    public string PriceRange { get; set; } = string.Empty;

    [Required]
    public string OpeningHours { get; set; } = string.Empty;

    public string TimeSlots { get; set; } = string.Empty;

    public string SetMenuDetails { get; set; } = string.Empty;

    public string DietaryOptions { get; set; } = string.Empty;

    public string GroupSizeCategory { get; set; } = string.Empty;

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Seating capacity must be at least 1."
    )]
    public int SeatingCapacity { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public string? Images { get; set; }
}