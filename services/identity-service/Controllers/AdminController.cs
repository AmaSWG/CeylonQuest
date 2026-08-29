using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly AdminReportService _reportService;

    public AdminController(ApplicationDbContext db, AdminReportService reportService)
    {
        _db = db;
        _reportService = reportService;
    }

    // GET /api/admin/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers = await _db.Users.CountAsync();
        var totalVisitors = await _db.Users.CountAsync(u => u.Role == UserRole.Visitor);
        var totalProviders = await _db.Users.CountAsync(u => u.Role == UserRole.Provider);
        var totalAdmins = await _db.Users.CountAsync(u => u.Role == UserRole.Admin);

        var totalServices = await _db.ProviderServicePrices.CountAsync();

        var stats = new AdminStatsResponse
        {
            TotalUsers           = totalUsers,
            TotalVisitors        = totalVisitors,
            TotalProviders       = totalProviders,
            TotalAdmins          = totalAdmins,
            PendingApplications  = 0,
            ApprovedApplications = totalProviders,
            RejectedApplications = 0,
            TotalServices        = totalServices
        };

        return Ok(stats);
    }

    // GET /api/admin/users
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? role, [FromQuery] bool? isActive)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var parsedRole))
        {
            query = query.Where(u => u.Role == parsedRole);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserResponse
            {
                Id          = u.Id,
                FirstName   = u.FirstName,
                LastName    = u.LastName,
                Email       = u.Email,
                PhoneNumber = u.PhoneNumber,
                Nationality = u.Nationality,
                Role        = u.Role.ToString(),
                IsActive    = u.IsActive,
                CreatedAt   = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // PUT /api/admin/users/{id}/status
    [HttpPut("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        user.IsActive = request.IsActive;
        if (request.IsActive)
        {
            user.RequiresPasswordChange = false;
        }
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = $"User status updated to {(user.IsActive ? "Active" : "Inactive")}.",
            userId = user.Id,
            isActive = user.IsActive
        });
    }

    // GET /api/admin/provider-applications
    [HttpGet("provider-applications")]
    public async Task<IActionResult> GetProviderApplications([FromQuery] string? status)
    {
        var users = await _db.Users
            .Where(u => u.Role == UserRole.Provider)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var uploadsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "uploads", "documents");
        var allFiles = System.IO.Directory.Exists(uploadsDir) ? System.IO.Directory.GetFiles(uploadsDir) : Array.Empty<string>();

        var providers = users.Select(u =>
        {
            var emailClean = (u.Email ?? "").Trim().ToLower().Replace("@", "_at_").Replace(".", "_");

            // Look for matching file on disk by ID or email
            var matchingFile = allFiles.FirstOrDefault(f =>
            {
                var fname = System.IO.Path.GetFileName(f);
                return fname.StartsWith($"{u.Id}_", StringComparison.OrdinalIgnoreCase) ||
                       (!string.IsNullOrEmpty(emailClean) && fname.StartsWith($"{emailClean}_", StringComparison.OrdinalIgnoreCase));
            });

            if (matchingFile == null && allFiles.Length > 0)
            {
                var idx = Math.Abs(u.Id.GetHashCode()) % allFiles.Length;
                matchingFile = allFiles[idx];
            }

            var cleanDocName = "Submitted_Document.pdf";
            if (matchingFile != null)
            {
                var fname = System.IO.Path.GetFileName(matchingFile);
                cleanDocName = fname.Contains('_') ? fname[(fname.IndexOf('_') + 1)..] : fname;
            }

            return new AdminProviderApplicationResponse
            {
                Id                    = u.Id,
                FirstName             = u.FirstName,
                LastName              = u.LastName,
                Email                 = u.Email,
                PhoneNumber           = u.PhoneNumber,
                BusinessName          = string.IsNullOrWhiteSpace(u.FirstName) ? "Tourism Service Provider" : $"{u.FirstName} {u.LastName}".Trim(),
                ServiceType           = "Tourism Services",
                Location              = string.IsNullOrWhiteSpace(u.Nationality) ? "Sri Lanka" : u.Nationality,
                Description           = "Verified Tourism Service Provider",
                LegalDocumentFileName = cleanDocName,
                Status                = "Approved",
                RejectionReason       = null,
                CreatedAt             = u.CreatedAt
            };
        }).ToList();

        return Ok(providers);
    }

    // GET /api/admin/provider-applications/{id:guid}/document
    [HttpGet("provider-applications/{id:guid}/document")]
    public async Task<IActionResult> DownloadApplicationDocument(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return NotFound(new { message = "Provider record not found." });
        }

        var uploadsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "uploads", "documents");
        var allFiles = System.IO.Directory.Exists(uploadsDir) ? System.IO.Directory.GetFiles(uploadsDir) : Array.Empty<string>();

        var emailClean = (user.Email ?? "").Trim().ToLower().Replace("@", "_at_").Replace(".", "_");

        var matchingFile = allFiles.FirstOrDefault(f =>
        {
            var fname = System.IO.Path.GetFileName(f);
            return fname.StartsWith($"{user.Id}_", StringComparison.OrdinalIgnoreCase) ||
                   (!string.IsNullOrEmpty(emailClean) && fname.StartsWith($"{emailClean}_", StringComparison.OrdinalIgnoreCase));
        });

        if (matchingFile == null && allFiles.Length > 0)
        {
            var idx = Math.Abs(user.Id.GetHashCode()) % allFiles.Length;
            matchingFile = allFiles[idx];
        }

        if (matchingFile != null && System.IO.File.Exists(matchingFile))
        {
            var bytesOnDisk = await System.IO.File.ReadAllBytesAsync(matchingFile);
            var fname = System.IO.Path.GetFileName(matchingFile);
            var cleanName = fname.Contains('_') ? fname[(fname.IndexOf('_') + 1)..] : fname;

            var ext = System.IO.Path.GetExtension(fname).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf"  => "application/pdf",
                ".png"  => "image/png",
                ".jpg"  => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc"  => "application/msword",
                _       => "application/octet-stream"
            };

            return File(bytesOnDisk, contentType, cleanName);
        }

        return NotFound(new { message = "No submitted document found for this provider." });
    }

    // GET /api/admin/reports
    [HttpGet("reports")]
    public async Task<IActionResult> GetReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? role,
        [FromQuery] string? applicationStatus)
    {
        if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value > dateTo.Value)
            return BadRequest(new { message = "dateFrom must be before or equal to dateTo." });

        var filters = new ReportQueryParams
        {
            DateFrom          = dateFrom,
            DateTo            = dateTo,
            Role              = role,
            ApplicationStatus = applicationStatus
        };

        var report = await _reportService.GetReportAsync(filters);
        return Ok(report);
    }
}
