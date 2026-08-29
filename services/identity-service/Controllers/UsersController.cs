using System.Security.Claims;
using IdentityService.DTOs;
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserProfileService _profileService;
    private readonly IWebHostEnvironment _env;

    public UsersController(UserProfileService profileService, IWebHostEnvironment env)
    {
        _profileService = profileService;
        _env = env;
    }

    // GET /api/users/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var profile = await _profileService.GetProfileAsync(userId.Value);
            return Ok(profile);
        }
        catch (UserNotFoundException)
        {
            return NotFound(new { message = "User profile not found." });
        }
    }

    // PUT /api/users/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var profile = await _profileService.UpdateProfileAsync(userId.Value, request);
            return Ok(new { message = "Profile updated successfully.", profile });
        }
        catch (UserNotFoundException)
        {
            return NotFound(new { message = "User profile not found." });
        }
    }

    // GET /api/users/avatar/{fileName} and /uploads/avatars/{fileName}
    [HttpGet("avatar/{fileName}")]
    [HttpGet("/uploads/avatars/{fileName}")]
    [AllowAnonymous]
    public IActionResult GetAvatar(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        var avatarsFolder = Path.Combine(_env.ContentRootPath, "uploads", "avatars");
        var filePath = Path.Combine(avatarsFolder, safeFileName);
        if (!System.IO.File.Exists(filePath)) return NotFound();

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
        return PhysicalFile(filePath, contentType);
    }

    // POST /api/users/me/profile-picture
    [HttpPost("me/profile-picture")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Please select an image file to upload." });
        }

        try
        {
            var baseDir = _env.ContentRootPath;
            var profile = await _profileService.UploadProfilePictureAsync(userId.Value, file, baseDir);
            return Ok(new { message = "Profile picture updated successfully.", profile, profilePictureUrl = profile.ProfilePictureUrl });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UserNotFoundException)
        {
            return NotFound(new { message = "User not found." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to upload image.", details = ex.Message });
        }
    }

    // DELETE /api/users/me/profile-picture
    [HttpDelete("me/profile-picture")]
    public async Task<IActionResult> DeleteProfilePicture()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var baseDir = _env.ContentRootPath;
            var profile = await _profileService.RemoveProfilePictureAsync(userId.Value, baseDir);
            return Ok(new { message = "Profile picture removed successfully.", profile });
        }
        catch (UserNotFoundException)
        {
            return NotFound(new { message = "User not found." });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) 
               ?? User.FindFirstValue("sub")
               ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
