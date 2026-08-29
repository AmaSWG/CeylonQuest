using System.Security.Claims;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/provider/info")]
[Authorize(Roles = "Provider")]
public class ProviderInfoController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProviderInfoController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetProviderInfo()
    {
        var providerId = GetProviderId();
        if (providerId is null) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == providerId.Value);
        if (user is null) return NotFound(new { message = "Provider not found." });

        var response = new ProviderInfoResponse
        {
            UserId             = user.Id,
            FirstName          = user.FirstName,
            LastName           = user.LastName,
            Email              = user.Email,
            PhoneNumber        = user.PhoneNumber,
            BusinessName       = $"{user.FirstName} {user.LastName}".Trim(),
            ServiceType        = "Tourism Service Provider",
            Location           = user.Nationality,
            Description        = "Tourism Services",
            VerificationStatus = "Verified",
            MemberSince        = user.CreatedAt
        };

        return Ok(response);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProviderInfo([FromBody] UpdateProviderInfoRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var providerId = GetProviderId();
        if (providerId is null) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == providerId.Value);
        if (user is null) return NotFound(new { message = "Provider not found." });

        var response = new ProviderInfoResponse
        {
            UserId             = user.Id,
            FirstName          = user.FirstName,
            LastName           = user.LastName,
            Email              = user.Email,
            PhoneNumber        = user.PhoneNumber,
            BusinessName       = request.BusinessName.Trim(),
            ServiceType        = request.ServiceType.Trim(),
            Location           = request.Location.Trim(),
            Description        = request.Description.Trim(),
            VerificationStatus = "Verified",
            MemberSince        = user.CreatedAt
        };

        return Ok(response);
    }

    private Guid? GetProviderId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
