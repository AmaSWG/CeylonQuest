using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class UpdateProfileRequest
{
    private const string NotBlankPattern = @"^(?!\s*$).+";

    [Required(ErrorMessage = "First name is required.")]
    [RegularExpression(NotBlankPattern, ErrorMessage = "First name cannot be blank.")]
    [MaxLength(100, ErrorMessage = "First name must be 100 characters or fewer.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [RegularExpression(NotBlankPattern, ErrorMessage = "Last name cannot be blank.")]
    [MaxLength(100, ErrorMessage = "Last name must be 100 characters or fewer.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nationality is required.")]
    [RegularExpression(NotBlankPattern, ErrorMessage = "Nationality cannot be blank.")]
    [MaxLength(100, ErrorMessage = "Nationality must be 100 characters or fewer.")]
    public string Nationality { get; set; } = string.Empty;
}
