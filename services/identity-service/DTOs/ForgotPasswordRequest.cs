using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}
