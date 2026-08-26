using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
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

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
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

        var fileName = string.IsNullOrWhiteSpace(application.LegalDocumentFileName)
            ? $"{application.BusinessName.Replace(" ", "_")}_registration_doc.txt"
            : application.LegalDocumentFileName;

        // Check if an uploaded physical file exists in uploads folder
        var uploadsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "uploads", "documents");
        var filePath = System.IO.Path.Combine(uploadsDir, fileName);

        if (System.IO.File.Exists(filePath))
        {
            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var contentType = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf"
                            : fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png"
                            : fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg"
                            : "application/octet-stream";
            return File(bytes, contentType, fileName);
        }

        // Generate formatted verification document with registered application details
        var docContent = $@"================================================================================
CEYLONQUEST TOURISM PLATFORM — PROVIDER REGISTRATION DOCUMENT
================================================================================

Application Reference ID: {application.Id}
Submission Date:          {application.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC
Application Status:       {application.Status}

BUSINESS PROFILE:
-----------------
Business Name:            {application.BusinessName}
Service Category:         {application.ServiceType}
Operating Location:       {application.Location}

APPLICANT INFORMATION:
----------------------
Owner / Representative:   {application.FirstName} {application.LastName}
Official Email:           {application.Email}
Contact Phone:            {application.PhoneNumber}

BUSINESS DESCRIPTION & DECLARATION:
-----------------------------------
{application.Description}

ATTACHED DOCUMENT METADATA:
---------------------------
File Reference:           {application.LegalDocumentFileName ?? "Standard Business Verification Record"}
Verification Authority:   CeylonQuest Tourism Accreditation Board
================================================================================
";

        var contentBytes = System.Text.Encoding.UTF8.GetBytes(docContent);
        var downloadName = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? fileName.Replace(".pdf", ".txt") : fileName;
        if (!downloadName.Contains('.')) downloadName += ".txt";

        return File(contentBytes, "text/plain; charset=utf-8", downloadName);
    }
}
