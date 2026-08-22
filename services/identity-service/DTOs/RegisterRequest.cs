using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class RegisterRequest
{
    // Password requirements are surfaced to the client via this message so the UI can display them (Story 1.1, Scenario 3).
    public const string PasswordRequirementsMessage =
        "Password must be at least 8 characters long and include an uppercase letter, a lowercase letter, a number, and a special character.";

    private const string NotBlankPattern = @"^(?!\s*$).+";
    private const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z\s]).{8,}$";

    [Required(ErrorMessage = "First name is required.")]
    [RegularExpression(NotBlankPattern, ErrorMessage = "First name cannot be blank.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [RegularExpression(NotBlankPattern, ErrorMessage = "Last name cannot be blank.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nationality is required.")]
    [RegularExpression(NotBlankPattern, ErrorMessage = "Nationality cannot be blank.")]
    public string Nationality { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [RegularExpression(PasswordPattern, ErrorMessage = PasswordRequirementsMessage)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Registration type is required.")]
    public RegistrationType RegistrationType { get; set; }
}

public enum RegistrationType
{
    Visitor,
    ServiceProvider
}