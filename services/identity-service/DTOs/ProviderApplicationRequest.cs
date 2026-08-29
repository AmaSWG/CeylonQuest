using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace IdentityService.DTOs;

public class ProviderApplicationRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    [Required]
    public string BusinessName { get; set; } = string.Empty;
    [Required]
    public string ServiceType { get; set; } = string.Empty;
    [Required]
    public string Location { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;

    public IFormFile? LegalDocument { get; set; }

    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
}
