using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class ProviderApplicationRequest
{
    // Account contact
    [Required]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    public string LastName { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    // Business
    [Required]
    public string BusinessName { get; set; } = string.Empty;
    [Required]
    public string ServiceType { get; set; } = string.Empty;
    [Required]
    public string Location { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;

    // File upload: frontend will send filename only for now
    public string? LegalDocumentFileName { get; set; }

    // NOTE: frontend includes password fields but backend will NOT store passwords here.
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
}
