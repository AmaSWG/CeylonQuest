using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Password reset token is required.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [RegularExpression(
        RegisterRequest.PasswordPattern,
        ErrorMessage = RegisterRequest.PasswordRequirementsMessage)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
