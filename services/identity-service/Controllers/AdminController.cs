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

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers = await _db.Users.CountAsync();
        var totalVisitors = await _db.Users.CountAsync(u => u.Role == UserRole.Visitor);
        var totalProviders = await _db.Users.CountAsync(u => u.Role == UserRole.Provider);
        var totalAdmins = await _db.Users.CountAsync(u => u.Role == UserRole.Admin);

        var totalServices = await _db.ProviderServicePrices.CountAsync();

        var approvedEmails = await _db.Users
            .Where(u => u.Role == UserRole.Provider)
            .Select(u => u.Email.ToLower())
            .ToListAsync();

        var uploadsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "uploads", "documents");
        var pending = LoadPendingApplications(approvedEmails, uploadsDir);

        var stats = new AdminStatsResponse
        {
            TotalUsers           = totalUsers,
            TotalVisitors        = totalVisitors,
            TotalProviders       = totalProviders,
            TotalAdmins          = totalAdmins,
            PendingApplications  = pending.Count,
            ApprovedApplications = totalProviders,
            RejectedApplications = 0,
            TotalServices        = totalServices
        };

        return Ok(stats);
    }

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

    [HttpGet("provider-applications")]
    public async Task<IActionResult> GetProviderApplications([FromQuery] string? status)
    {
        var uploadsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "uploads", "documents");
        var allFiles = System.IO.Directory.Exists(uploadsDir) ? System.IO.Directory.GetFiles(uploadsDir) : Array.Empty<string>();

        var approvedUsers = await _db.Users
            .Where(u => u.Role == UserRole.Provider)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var approvedEmails = approvedUsers.Select(u => u.Email.ToLower()).ToList();

        var approvedList = approvedUsers.Select(u =>
        {
            var emailClean = (u.Email ?? "").Trim().ToLower().Replace("@", "_at_").Replace(".", "_");

            var matchingFile = allFiles.FirstOrDefault(f =>
            {
                var fname = System.IO.Path.GetFileName(f);
                return !fname.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                       (fname.StartsWith($"{u.Id}_", StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(emailClean) && fname.StartsWith($"{emailClean}_", StringComparison.OrdinalIgnoreCase)));
            });

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

        var pendingList = LoadPendingApplications(approvedEmails, uploadsDir);

        var combined = pendingList.Concat(approvedList).OrderByDescending(a => a.CreatedAt).ToList();

        if (!string.IsNullOrWhiteSpace(status))
        {
            combined = combined.Where(a => a.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Ok(combined);
    }

    [HttpGet("provider-applications/{id:guid}/document")]
    public async Task<IActionResult> DownloadApplicationDocument(Guid id)
    {
        var uploadsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "uploads", "documents");
        var allFiles = System.IO.Directory.Exists(uploadsDir) ? System.IO.Directory.GetFiles(uploadsDir) : Array.Empty<string>();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        var email = user?.Email;

        if (string.IsNullOrEmpty(email) && System.IO.Directory.Exists(uploadsDir))
        {
            var jsonFiles = System.IO.Directory.GetFiles(uploadsDir, "*_application.json");
            foreach (var jf in jsonFiles)
            {
                try
                {
                    var text = await System.IO.File.ReadAllTextAsync(jf);
                    using var doc = System.Text.Json.JsonDocument.Parse(text);
                    if (doc.RootElement.TryGetProperty("ApplicationId", out var idProp) &&
                        Guid.TryParse(idProp.GetString(), out var parsedId) &&
                        parsedId == id)
                    {
                        if (doc.RootElement.TryGetProperty("Email", out var em))
                        {
                            email = em.GetString();
                            break;
                        }
                    }
                }
                catch { }
            }
        }

        var emailClean = (email ?? "").Trim().ToLower().Replace("@", "_at_").Replace(".", "_");

        var matchingFile = allFiles.FirstOrDefault(f =>
        {
            var fname = System.IO.Path.GetFileName(f);
            return !fname.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                   (fname.StartsWith($"{id}_", StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(emailClean) && fname.StartsWith($"{emailClean}_", StringComparison.OrdinalIgnoreCase)));
        });

        if (matchingFile == null && allFiles.Length > 0)
        {
            var nonJson = allFiles.Where(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (nonJson.Length > 0)
            {
                var idx = Math.Abs(id.GetHashCode()) % nonJson.Length;
                matchingFile = nonJson[idx];
            }
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

        return NotFound(new { message = "No submitted document found for this application." });
    }

    private static List<AdminProviderApplicationResponse> LoadPendingApplications(List<string> approvedEmails, string uploadsDir)
    {
        var list = new List<AdminProviderApplicationResponse>();
        if (!System.IO.Directory.Exists(uploadsDir)) return list;

        var jsonFiles = System.IO.Directory.GetFiles(uploadsDir, "*_application.json");
        var allFiles = System.IO.Directory.GetFiles(uploadsDir);

        foreach (var jsonFile in jsonFiles)
        {
            try
            {
                var content = System.IO.File.ReadAllText(jsonFile);
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                var root = doc.RootElement;

                var email = root.TryGetProperty("Email", out var e) ? e.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(email)) continue;

                var emailLower = email.Trim().ToLower();
                if (approvedEmails.Contains(emailLower)) continue;

                var appId = root.TryGetProperty("ApplicationId", out var idProp) && Guid.TryParse(idProp.GetString(), out var parsedId)
                    ? parsedId
                    : Guid.NewGuid();

                var bName = root.TryGetProperty("BusinessName", out var b) ? b.GetString() ?? "Tourism Service Provider" : "Tourism Service Provider";
                var sType = root.TryGetProperty("ServiceType", out var s) ? s.GetString() ?? "Tourism Services" : "Tourism Services";
                var loc = root.TryGetProperty("Location", out var l) ? l.GetString() ?? "Sri Lanka" : "Sri Lanka";
                var desc = root.TryGetProperty("Description", out var d) ? d.GetString() ?? "" : "";
                var subAt = root.TryGetProperty("SubmittedAt", out var dt) ? dt.GetDateTime() : System.IO.File.GetCreationTimeUtc(jsonFile);

                var emailClean = emailLower.Replace("@", "_at_").Replace(".", "_");

                var docFile = allFiles.FirstOrDefault(f =>
                {
                    var fname = System.IO.Path.GetFileName(f);
                    return !fname.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                           (fname.StartsWith($"{appId}_", StringComparison.OrdinalIgnoreCase) ||
                            fname.StartsWith($"{emailClean}_", StringComparison.OrdinalIgnoreCase));
                });

                var cleanDocName = "Submitted_Document.pdf";
                if (docFile != null)
                {
                    var fname = System.IO.Path.GetFileName(docFile);
                    cleanDocName = fname.Contains('_') ? fname[(fname.IndexOf('_') + 1)..] : fname;
                }

                list.Add(new AdminProviderApplicationResponse
                {
                    Id                    = appId,
                    FirstName             = "",
                    LastName              = "",
                    Email                 = emailLower,
                    PhoneNumber           = "",
                    BusinessName          = bName,
                    ServiceType           = sType,
                    Location              = loc,
                    Description           = desc,
                    LegalDocumentFileName = cleanDocName,
                    Status                = "Pending",
                    RejectionReason       = null,
                    CreatedAt             = subAt
                });
            }
            catch { }
        }

        return list;
    }

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
