using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace IdentityService.DTOs;

public class ProviderApplicationRequest
{
    // Account contact (provided during post-approval OTP profile activation)
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    // Business
    [Required]
    public string BusinessName { get; set; } = string.Empty;
    [Required]
    public string ServiceType { get; set; } = string.Empty;
    [Required]
    public string Location { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;

    // The actual uploaded file (PDF / JPG / PNG)
    public IFormFile? LegalDocument { get; set; }

    // Password fields — validated on the frontend; not stored here
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
}
