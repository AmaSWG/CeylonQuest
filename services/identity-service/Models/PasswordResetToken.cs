namespace IdentityService.Models;

/// <summary>
/// Represents a password reset token issued for a user's account recovery.
/// Tokens are single-use, expire after 30 minutes, and are stored as hashes (not plaintext).
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; set; }

    /// <summary>
    /// The user requesting password reset.
    /// </summary>
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// The hash of the actual reset token (never store plaintext token).
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// When this token expires and becomes invalid.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When this token was used to reset the password (null = not yet used).
    /// Once used, the token cannot be reused.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// When this token was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
