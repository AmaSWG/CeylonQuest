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

        var pendingApps = await _db.ProviderApplications.CountAsync(a => a.Status == ProviderApplicationStatus.Pending);
        var approvedApps = await _db.ProviderApplications.CountAsync(a => a.Status == ProviderApplicationStatus.Approved);
        var rejectedApps = await _db.ProviderApplications.CountAsync(a => a.Status == ProviderApplicationStatus.Rejected);

        var totalServices = await _db.ProviderServicePrices.CountAsync();

        var stats = new AdminStatsResponse
        {
            TotalUsers           = totalUsers,
            TotalVisitors        = totalVisitors,
            TotalProviders       = totalProviders,
            TotalAdmins          = totalAdmins,
            PendingApplications  = pendingApps,
            ApprovedApplications = approvedApps,
            RejectedApplications = rejectedApps,
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
        var query = _db.ProviderApplications.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProviderApplicationStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(a => a.Status == parsedStatus);
        }

        var applications = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AdminProviderApplicationResponse
            {
                Id                    = a.Id,
                FirstName             = a.FirstName,
                LastName              = a.LastName,
                Email                 = a.Email,
                PhoneNumber           = a.PhoneNumber,
                BusinessName          = a.BusinessName,
                ServiceType           = a.ServiceType,
                Location              = a.Location,
                Description           = a.Description,
                LegalDocumentFileName = a.LegalDocumentFileName,
                Status                = a.Status.ToString(),
                RejectionReason       = a.RejectionReason,
                CreatedAt             = a.CreatedAt
            })
            .ToListAsync();

        return Ok(applications);
    }

    // GET /api/admin/provider-applications/{id:guid}/document
    [HttpGet("provider-applications/{id:guid}/document")]
    public async Task<IActionResult> DownloadApplicationDocument(Guid id)
    {
        var application = await _db.ProviderApplications.FirstOrDefaultAsync(a => a.Id == id);
        if (application is null)
        {
            return NotFound(new { message = "Provider application not found." });
        }

        // storedFileName is the name on disk (e.g. "guid_original.pdf")
        var storedFileName = application.LegalDocumentFileName;

        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            return NotFound(new { message = "No document was submitted with this application." });
        }

        var uploadsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "uploads", "documents");
        var filePath   = System.IO.Path.Combine(uploadsDir, storedFileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "Document file not found on server." });
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

        // Determine content type from extension
        var ext = System.IO.Path.GetExtension(storedFileName).ToLowerInvariant();
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

        // Strip the leading {guid}_ prefix so the admin sees the original clean filename
        var downloadName = storedFileName;
        var underscoreIdx = storedFileName.IndexOf('_');
        if (underscoreIdx > 0 && Guid.TryParse(storedFileName[..underscoreIdx], out _))
        {
            downloadName = storedFileName[(underscoreIdx + 1)..];
        }

        return File(bytes, contentType, downloadName);
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
