using System.ComponentModel.DataAnnotations;

namespace ProviderCatalogService.DTOs;

public class UpdateActivityListingRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Required]
    public string Unit { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Max participants must be at least 1.")]
    public int MaxParticipants { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public string Duration { get; set; } = "2 Hours";

    [Required(ErrorMessage = "Available operating days are required (e.g. Daily, Mon–Fri, Weekends).")]
    public string AvailableDays { get; set; } = "Daily";

    public string TimeSlots { get; set; } = "[]";

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public string? Images { get; set; }
}