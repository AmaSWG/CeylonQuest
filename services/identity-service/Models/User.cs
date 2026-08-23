namespace IdentityService.Models;

public class User
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Nationality { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Visitor;

    // One-time password issued for account activation (e.g. after provider approval).
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiresAt { get; set; }

    // Forces the user to set a new password on next login (e.g. newly activated provider accounts).
    public bool RequiresPasswordChange { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum UserRole
{
    Visitor,
    Provider,
    Admin
}