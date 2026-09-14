using IdentityService.Data;
using IdentityService.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Storage;

namespace IdentityService.Services;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(string? message = null) : base(message) { }
}

public class UserProfileService
{
    private readonly ApplicationDbContext _db;
    private readonly IBlobStorageService _blobStorage;
    private readonly string _avatarContainer;

    public UserProfileService(ApplicationDbContext db, IBlobStorageService blobStorage, IConfiguration configuration)
    {
        _db = db;
        _blobStorage = blobStorage;
        _avatarContainer = configuration["AzureStorage:ProfilePicturesContainer"] ?? "profile-images";
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

    public async Task<UserProfileResponse> UploadProfilePictureAsync(Guid userId, IFormFile file)
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

        // 1. Delete previous avatar from Azure if one exists
        if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
        {
            try
            {
                await _blobStorage.DeleteAsync(user.ProfilePictureUrl, _avatarContainer);
            }
            catch { /* Ignore if old blob is not found */ }
        }

        // 2. Upload new avatar stream to Azure Blob Container
        var blobName = $"{userId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{ext}";
        var contentType = (file.Headers != null && !string.IsNullOrWhiteSpace(file.ContentType))
            ? file.ContentType
            : (ext == ".png" ? "image/png" : ext == ".webp" ? "image/webp" : "image/jpeg");

        await using var stream = file.OpenReadStream();
        var blobUrl = await _blobStorage.UploadAsync(stream, blobName, _avatarContainer, contentType);

        // 3. Save direct Azure Blob URL in database
        user.ProfilePictureUrl = blobUrl;
        await _db.SaveChangesAsync();

        return MapToResponse(user);
    }

    public async Task<UserProfileResponse> RemoveProfilePictureAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            throw new UserNotFoundException($"User {userId} not found.");

        if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
        {
            try
            {
                await _blobStorage.DeleteAsync(user.ProfilePictureUrl, _avatarContainer);
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