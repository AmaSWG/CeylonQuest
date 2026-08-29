using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class ProviderActivateRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Otp { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}
