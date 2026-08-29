using System.Security.Cryptography;
using IdentityService.Data;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services;

public class PasswordResetTokenService
{
    private readonly ApplicationDbContext _db;
    private const int TokenLengthBytes = 32;
    private const int DefaultExpiryMinutes = 30;

    public PasswordResetTokenService(ApplicationDbContext db)
    {
        _db = db;
    }

    public (string token, string hash) GenerateToken()
    {
        var randomBytes = new byte[TokenLengthBytes];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var token = Base64UrlEncode(randomBytes);

        var hasher = new PasswordHasher<User>();
        var tempUser = new User { Id = Guid.Empty };
        var hash = hasher.HashPassword(tempUser, token);

        return (token, hash);
    }

    public async Task<(string token, PasswordResetToken resetToken)> CreateTokenAsync(Guid userId)
    {
        var existingTokens = await _db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var oldToken in existingTokens)
        {
            oldToken.UsedAt = DateTime.UtcNow;
        }

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

    public async Task<PasswordResetToken?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var candidateTokens = await _db.PasswordResetTokens
            .Where(t => t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var hasher = new PasswordHasher<User>();
        var tempUser = new User { Id = Guid.Empty };

        foreach (var dbToken in candidateTokens)
        {
            var result = hasher.VerifyHashedPassword(tempUser, dbToken.TokenHash, token);
            if (result == PasswordVerificationResult.Success)
            {
                return await _db.PasswordResetTokens
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == dbToken.Id);
            }
        }

        return null;
    }

    public async Task MarkAsUsedAsync(PasswordResetToken token)
    {
        token.UsedAt = DateTime.UtcNow;
        _db.PasswordResetTokens.Update(token);
        await _db.SaveChangesAsync();
    }

    private static string Base64UrlEncode(byte[] input)
    {
        var output = Convert.ToBase64String(input);
        return output
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
