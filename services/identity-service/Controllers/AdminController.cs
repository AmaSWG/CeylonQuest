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
            ApprovedApplications = 0,
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
    public IActionResult GetProviderApplications([FromQuery] string? status)
    {
        // Provider applications are managed by the Provider/Catalog Service
        return Ok(new List<AdminProviderApplicationResponse>());
    }

    // GET /api/admin/provider-applications/{id:guid}/document
    [HttpGet("provider-applications/{id:guid}/document")]
    public IActionResult DownloadApplicationDocument(Guid id)
    {
        return NotFound(new { message = "Provider applications and documents are managed by the Provider/Catalog Service." });
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
