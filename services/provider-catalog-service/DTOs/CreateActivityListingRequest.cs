using System.ComponentModel.DataAnnotations;

namespace ProviderCatalogService.DTOs;

public class CreateActivityListingRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    public string Unit { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;
	
	public int MaxParticipants { get; set; }

    public bool IsActive { get; set; } = true;
}