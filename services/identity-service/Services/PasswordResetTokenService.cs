using System.Security.Cryptography;
using IdentityService.Data;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services;

/// <summary>
/// Manages the lifecycle of password reset tokens:
/// - Generation: Cryptographically secure tokens
/// - Storage: Hashed tokens in the database
/// - Validation: Expiry, usage status, and existence
/// - Invalidation: Mark as used after successful reset
/// </summary>
public class PasswordResetTokenService
{
    private readonly ApplicationDbContext _db;
    private const int TokenLengthBytes = 32; // 256 bits
    private const int DefaultExpiryMinutes = 30;

    public PasswordResetTokenService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Generates a cryptographically secure reset token and its hash.
    /// </summary>
    /// <returns>Tuple of (plaintext token for sending, hash for storage).</returns>
    public (string token, string hash) GenerateToken()
    {
        // Generate cryptographically secure random bytes
        var randomBytes = new byte[TokenLengthBytes];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        // Convert to URL-safe Base64 string
        var token = Base64UrlEncode(randomBytes);

        // Hash the token for storage (similar to password hashing)
        var hasher = new PasswordHasher<User>();
        // We use a temporary user object just for hashing (similar to password hashing flow)
        var tempUser = new User { Id = Guid.Empty };
        var hash = hasher.HashPassword(tempUser, token);

        return (token, hash);
    }

    /// <summary>
    /// Creates a new password reset token for the given user.
    /// Old tokens for this user are automatically invalidated.
    /// Returns a tuple of (plaintext token, storedToken entity).
    /// </summary>
    public async Task<(string token, PasswordResetToken resetToken)> CreateTokenAsync(Guid userId)
    {
        // Invalidate any existing unused tokens for this user
        var existingTokens = await _db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var oldToken in existingTokens)
        {
            oldToken.UsedAt = DateTime.UtcNow; // Mark as "used" to invalidate
        }

        // Generate new token
        var (token, hash) = GenerateToken();

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(DefaultExpiryMinutes),
            UsedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        _db.PasswordResetTokens.Add(resetToken);
        await _db.SaveChangesAsync();

        return (token, resetToken);
    }

    /// <summary>
    /// Validates that a reset token is valid (exists, not expired, not used).
    /// Returns null if token is invalid for any reason.
    /// </summary>
    public async Task<PasswordResetToken?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        // Find all non-expired, non-used tokens
        var candidateTokens = await _db.PasswordResetTokens
            .Where(t => t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        // Use PasswordHasher to verify the token against stored hashes
        var hasher = new PasswordHasher<User>();
        var tempUser = new User { Id = Guid.Empty };

        foreach (var dbToken in candidateTokens)
        {
            var result = hasher.VerifyHashedPassword(tempUser, dbToken.TokenHash, token);
            if (result == PasswordVerificationResult.Success)
            {
                // Load full entity with User navigation
                return await _db.PasswordResetTokens
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == dbToken.Id);
            }
        }

        return null;
    }

    /// <summary>
    /// Marks a token as used after successful password reset.
    /// Once used, a token cannot be reused.
    /// </summary>
    public async Task MarkAsUsedAsync(PasswordResetToken token)
    {
        token.UsedAt = DateTime.UtcNow;
        _db.PasswordResetTokens.Update(token);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Helper: Convert random bytes to URL-safe Base64 string.
    /// </summary>
    private static string Base64UrlEncode(byte[] input)
    {
        var output = Convert.ToBase64String(input);
        // Convert to URL-safe Base64 (replace + with -, / with _)
        return output
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('='); // Remove padding
    }
}
