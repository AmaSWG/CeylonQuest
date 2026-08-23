using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Services;

public class AuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<LoginResponse> AuthenticateAsync(LoginRequest req)
    {
        var emailLower = req.Email.ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);
        // Generic failure for not found or wrong password
        if (user == null) throw new UnauthorizedAccessException("Invalid credentials");

        // If there's a provider application for this email (pending or approved), disallow password login here.
        var hasProviderApp = await _db.ProviderApplications
            .AnyAsync(p => p.Email.ToLower() == emailLower && p.Status != IdentityService.Models.ProviderApplicationStatus.Rejected);
        if (hasProviderApp)
        {
            // Providers must use the separate provider login (OTP) flow. Deny standard password login.
            throw new UnauthorizedAccessException("Provider applicants must use the provider login flow");
        }

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is not active");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        if (result == PasswordVerificationResult.Failed) throw new UnauthorizedAccessException("Invalid credentials");

        // create token
        var key = _config["Jwt:Key"] ?? "dev_secret_do_not_use_in_production_please_change_which_is_long_enough";
        var issuer = _config["Jwt:Issuer"] ?? "CeylonQuest";
        var audience = _config["Jwt:Audience"] ?? "CeylonQuestAudience";
        var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var m) ? m : 60;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        // Ensure signing key meets minimum HMAC-SHA256 length by deriving a 256-bit key.
        using var sha = System.Security.Cryptography.SHA256.Create();
        var keyBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResponse
        {
            AccessToken = tokenString,
            Role = user.Role.ToString()
        };
    }
}
