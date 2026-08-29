using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class ProviderServicePriceRequest
{
    private const string NotBlankPattern = @"^(?!\s*$).+";

    [Required(ErrorMessage = "Service name is required.")]
    [RegularExpression(NotBlankPattern, ErrorMessage = "Service name cannot be blank.")]
    [MaxLength(200, ErrorMessage = "Service name must be 200 characters or fewer.")]
    public string ServiceName { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description must be 500 characters or fewer.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price per unit is required.")]
    [Range(0.01, 1_000_000, ErrorMessage = "Price must be between 0.01 and 1,000,000.")]
    public decimal PricePerUnit { get; set; }

    [Required(ErrorMessage = "Unit is required.")]
    [RegularExpression(NotBlankPattern, ErrorMessage = "Unit cannot be blank.")]
    [MaxLength(50, ErrorMessage = "Unit must be 50 characters or fewer.")]
    public string Unit { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
