using IdentityService.Data;
using IdentityService.DTOs;
using Microsoft.AspNetCore.Http;
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

        return MapToResponse(user);
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

        if (request.ProfilePictureUrl != null)
        {
            user.ProfilePictureUrl = string.IsNullOrWhiteSpace(request.ProfilePictureUrl) ? null : request.ProfilePictureUrl.Trim();
        }

        await _db.SaveChangesAsync();

        return MapToResponse(user);
    }

    public async Task<UserProfileResponse> UploadProfilePictureAsync(Guid userId, IFormFile file, string baseDirectory)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No image file was uploaded.");

        const long maxSizeBytes = 5 * 1024 * 1024; // 5 MB
        if (file.Length > maxSizeBytes)
            throw new ArgumentException("Image file size must be 5 MB or smaller.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExtensions.Contains(ext))
            throw new ArgumentException("Invalid image format. Only JPG, PNG, and WebP are allowed.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            throw new UserNotFoundException($"User {userId} not found.");

        var avatarsFolder = Path.Combine(baseDirectory, "uploads", "avatars");
        Directory.CreateDirectory(avatarsFolder);

        // Clean up previous uploaded avatar file if local
        if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
        {
            try
            {
                var oldFileName = Path.GetFileName(user.ProfilePictureUrl);
                var oldFilePath = Path.Combine(avatarsFolder, oldFileName);
                if (File.Exists(oldFilePath)) File.Delete(oldFilePath);
            }
            catch { }
        }

        var newFileName = $"{userId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{ext}";
        var newFilePath = Path.Combine(avatarsFolder, newFileName);

        using (var stream = new FileStream(newFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        user.ProfilePictureUrl = $"/api/users/avatar/{newFileName}";
        await _db.SaveChangesAsync();

        return MapToResponse(user);
    }

    public async Task<UserProfileResponse> RemoveProfilePictureAsync(Guid userId, string baseDirectory)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            throw new UserNotFoundException($"User {userId} not found.");

        if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
        {
            try
            {
                var avatarsFolder = Path.Combine(baseDirectory, "uploads", "avatars");
                var oldFileName = Path.GetFileName(user.ProfilePictureUrl);
                var oldFilePath = Path.Combine(avatarsFolder, oldFileName);
                if (File.Exists(oldFilePath)) File.Delete(oldFilePath);
            }
            catch { }
        }

        user.ProfilePictureUrl = null;
        await _db.SaveChangesAsync();

        return MapToResponse(user);
    }

    private static UserProfileResponse MapToResponse(Models.User user)
    {
        return new UserProfileResponse
        {
            Id                = user.Id,
            FirstName         = user.FirstName,
            LastName          = user.LastName,
            Email             = user.Email,
            PhoneNumber       = user.PhoneNumber,
            Nationality       = user.Nationality,
            Role              = user.Role.ToString(),
            ProfilePictureUrl = user.ProfilePictureUrl,
            CreatedAt         = user.CreatedAt
        };
    }
}
