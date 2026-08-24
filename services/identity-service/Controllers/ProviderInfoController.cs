using System.Security.Claims;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Controllers;

/// <summary>
/// Returns and manages the authenticated Provider's business/service information,
/// combining their user record with their approved ProviderApplication record.
/// </summary>
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

    // GET /api/provider/info
    [HttpGet]
    public async Task<IActionResult> GetProviderInfo()
    {
        var providerId = GetProviderId();
        if (providerId is null) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == providerId.Value);
        if (user is null) return NotFound(new { message = "Provider not found." });

        // Look up the most recent application for this provider's email.
        var application = await _db.ProviderApplications
            .Where(a => a.Email.ToLower() == user.Email.ToLower())
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        var response = new ProviderInfoResponse
        {
            UserId             = user.Id,
            FirstName          = user.FirstName,
            LastName           = user.LastName,
            Email              = user.Email,
            PhoneNumber        = user.PhoneNumber,
            BusinessName       = application?.BusinessName ?? string.Empty,
            ServiceType        = application?.ServiceType  ?? string.Empty,
            Location           = application?.Location     ?? string.Empty,
            Description        = application?.Description  ?? string.Empty,
            VerificationStatus = application != null && application.Status == ProviderApplicationStatus.Approved ? "Verified" : (application?.Status.ToString() ?? "Verified"),
            MemberSince        = user.CreatedAt
        };

        return Ok(response);
    }

    // PUT /api/provider/info
    [HttpPut]
    public async Task<IActionResult> UpdateProviderInfo([FromBody] UpdateProviderInfoRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var providerId = GetProviderId();
        if (providerId is null) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == providerId.Value);
        if (user is null) return NotFound(new { message = "Provider not found." });

        var application = await _db.ProviderApplications
            .Where(a => a.Email.ToLower() == user.Email.ToLower())
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        if (application is null)
        {
            application = new ProviderApplication
            {
                Id           = Guid.NewGuid(),
                FirstName    = user.FirstName,
                LastName     = user.LastName,
                Email        = user.Email,
                PhoneNumber  = user.PhoneNumber,
                BusinessName = request.BusinessName.Trim(),
                ServiceType  = request.ServiceType.Trim(),
                Location     = request.Location.Trim(),
                Description  = request.Description.Trim(),
                Status       = ProviderApplicationStatus.Approved,
                CreatedAt    = DateTime.UtcNow
            };
            _db.ProviderApplications.Add(application);
        }
        else
        {
            application.BusinessName = request.BusinessName.Trim();
            application.ServiceType  = request.ServiceType.Trim();
            application.Location     = request.Location.Trim();
            application.Description  = request.Description.Trim();
        }

        await _db.SaveChangesAsync();

        var response = new ProviderInfoResponse
        {
            UserId             = user.Id,
            FirstName          = user.FirstName,
            LastName           = user.LastName,
            Email              = user.Email,
            PhoneNumber        = user.PhoneNumber,
            BusinessName       = application.BusinessName,
            ServiceType        = application.ServiceType,
            Location           = application.Location,
            Description        = application.Description,
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
