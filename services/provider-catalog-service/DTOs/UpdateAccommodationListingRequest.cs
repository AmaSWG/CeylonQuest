using System.ComponentModel.DataAnnotations;

namespace ProviderCatalogService.DTOs;

public class UpdateAccommodationListingRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string RoomType { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string PropertyType { get; set; } = "Boutique Hotel";

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required, Range(0.01, 5000000.00)]
    public decimal PricePerNight { get; set; }

    [Required, Range(1, 100)]
    public int MaxGuests { get; set; } = 2;

    [Required, StringLength(150)]
    public string BedDetails { get; set; } = "1 King Bed";

    [Range(1, 365)]
    public int MinStayNights { get; set; } = 1;

    [StringLength(500)]
    public string Amenities { get; set; } = "Free WiFi, AC, Breakfast Included";

    [StringLength(250)]
    public string BathroomDetails { get; set; } = "En-suite Private Bathroom";

    [Required, StringLength(3000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}