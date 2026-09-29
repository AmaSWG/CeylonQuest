using System.ComponentModel.DataAnnotations;

namespace ProviderCatalogService.DTOs;

public class CreateRestaurantListingRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CuisineType { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string DiningStyle { get; set; } = "Casual Dining";

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Range(
        0.01,
        1000000.00,
        ErrorMessage = "Price per person must be greater than 0."
    )]
    public decimal PricePerPerson { get; set; }

    public string PriceRange { get; set; } = "$$ (Moderate)";

    [Required]
    [StringLength(100)]
    public string OpeningHours { get; set; } = string.Empty;

    public string TimeSlots { get; set; } = string.Empty;

    [StringLength(3000)]
    public string SetMenuDetails { get; set; } = string.Empty;

    [StringLength(200)]
    public string DietaryOptions { get; set; } = "Standard";

    [StringLength(50)]
    public string GroupSizeCategory { get; set; } = "Table for Two";

    [Range(
        1,
        1000,
        ErrorMessage = "Seating capacity must be between 1 and 1000."
    )]
    public int SeatingCapacity { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Images { get; set; }
}