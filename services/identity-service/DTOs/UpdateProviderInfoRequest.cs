using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class UpdateProviderInfoRequest
{
    private const string NotBlankPattern = @"^(?!\s*$).+";

    [Required(ErrorMessage = "Business name is required.")]
    [RegularExpression(NotBlankPattern, ErrorMessage = "Business name cannot be blank.")]
    [MaxLength(200, ErrorMessage = "Business name must be 200 characters or fewer.")]
    public string BusinessName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Service type is required.")]
    [RegularExpression(NotBlankPattern, ErrorMessage = "Service type cannot be blank.")]
    [MaxLength(100, ErrorMessage = "Service type must be 100 characters or fewer.")]
    public string ServiceType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location is required.")]
    [RegularExpression(NotBlankPattern, ErrorMessage = "Location cannot be blank.")]
    [MaxLength(200, ErrorMessage = "Location must be 200 characters or fewer.")]
    public string Location { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description must be 1000 characters or fewer.")]
    public string Description { get; set; } = string.Empty;
}
