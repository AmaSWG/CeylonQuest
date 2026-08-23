using IdentityService.Data;
using IdentityService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(string? message = null) : base(message) { }
}

public class UserProfileService
{
    private readonly ApplicationDbContext _db;

    public UserProfileService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            throw new UserNotFoundException($"User {userId} not found.");

        return new UserProfileResponse
        {
            Id          = user.Id,
            FirstName   = user.FirstName,
            LastName    = user.LastName,
            Email       = user.Email,
            PhoneNumber = user.PhoneNumber,
            Nationality = user.Nationality,
            Role        = user.Role.ToString(),
            CreatedAt   = user.CreatedAt
        };
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            throw new UserNotFoundException($"User {userId} not found.");

        user.FirstName   = request.FirstName.Trim();
        user.LastName    = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber.Trim();
        user.Nationality = request.Nationality.Trim();

        await _db.SaveChangesAsync();

        return new UserProfileResponse
        {
            Id          = user.Id,
            FirstName   = user.FirstName,
            LastName    = user.LastName,
            Email       = user.Email,
            PhoneNumber = user.PhoneNumber,
            Nationality = user.Nationality,
            Role        = user.Role.ToString(),
            CreatedAt   = user.CreatedAt
        };
    }
}
