using System.Security.Claims;
using IdentityService.DTOs;
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserProfileService _profileService;

    public UsersController(UserProfileService profileService)
    {
        _profileService = profileService;
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

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) 
               ?? User.FindFirstValue("sub")
               ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
