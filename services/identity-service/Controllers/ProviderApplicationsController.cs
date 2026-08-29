using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProviderApplicationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProviderApplicationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost("/api/provider-applications")]
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB max
    public async Task<IActionResult> Create([FromForm] ProviderApplicationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var appId = Guid.NewGuid();
        var emailClean = (request.Email ?? "provider").Trim().ToLower().Replace("@", "_at_").Replace(".", "_");

        var uploadsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "uploads", "documents");
        System.IO.Directory.CreateDirectory(uploadsDir);

        var meta = new
        {
            ApplicationId = appId,
            BusinessName = request.BusinessName,
            Email = request.Email,
            ServiceType = request.ServiceType,
            Location = request.Location,
            Description = request.Description,
            SubmittedAt = DateTime.UtcNow
        };
        var metaJson = System.Text.Json.JsonSerializer.Serialize(meta);
        await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(uploadsDir, $"{emailClean}_application.json"), metaJson);

        if (request.LegalDocument is { Length: > 0 } file)
        {
            var ext = System.IO.Path.GetExtension(file.FileName);
            var safeOriginal = System.IO.Path.GetFileNameWithoutExtension(file.FileName)
                                   .Replace(" ", "_")
                                   .Replace("..", "");

            var savedFileName = $"{appId}_{safeOriginal}{ext}";
            var emailFileName = $"{emailClean}_{safeOriginal}{ext}";

            var filePath = System.IO.Path.Combine(uploadsDir, savedFileName);
            var emailFilePath = System.IO.Path.Combine(uploadsDir, emailFileName);

            await using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                await file.CopyToAsync(stream);
            }

            try
            {
                System.IO.File.Copy(filePath, emailFilePath, true);
            }
            catch { }
        }

        return StatusCode(201, new { message = "Application submitted successfully", applicationId = appId });
    }

    [HttpGet("status")]
    [HttpGet("/api/provider-applications/status")]
    public async Task<IActionResult> GetStatus([FromQuery] string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "Email address is required." });
        }

        var emailLower = email.Trim().ToLower();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);

        if (user != null)
        {
            if (user.Role != UserRole.Provider)
            {
                return Ok(new { found = false, message = "This email is registered as a Visitor account, not a Provider application." });
            }

            var name = $"{user.FirstName} {user.LastName}".Trim();
            return Ok(new ProviderApplicationStatusResponse
            {
                Email = user.Email,
                BusinessName = string.IsNullOrWhiteSpace(name) ? "Tourism Service Provider" : name,
                ServiceType = "Tourism Services",
                Status = "Approved",
                SubmittedAt = user.CreatedAt,
                Message = "Congratulations! Your provider account has been approved and verified. You can log in to your provider portal."
            });
        }

        var emailClean = emailLower.Replace("@", "_at_").Replace(".", "_");
        var uploadsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "uploads", "documents");

        if (System.IO.Directory.Exists(uploadsDir))
        {
            var metaPath = System.IO.Path.Combine(uploadsDir, $"{emailClean}_application.json");
            if (System.IO.File.Exists(metaPath))
            {
                try
                {
                    var json = await System.IO.File.ReadAllTextAsync(metaPath);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    var bName = root.TryGetProperty("BusinessName", out var b) ? b.GetString() : "Tourism Service Provider";
                    var sType = root.TryGetProperty("ServiceType", out var s) ? s.GetString() : "Tourism Services";
                    var subAt = root.TryGetProperty("SubmittedAt", out var d) ? d.GetDateTime() : DateTime.UtcNow;

                    return Ok(new ProviderApplicationStatusResponse
                    {
                        Email = emailLower,
                        BusinessName = string.IsNullOrWhiteSpace(bName) ? "Tourism Service Provider" : bName,
                        ServiceType = string.IsNullOrWhiteSpace(sType) ? "Tourism Services" : sType,
                        Status = "Pending",
                        SubmittedAt = subAt,
                        Message = "Your provider application has been received and is currently under review by our administration team."
                    });
                }
                catch { }
            }

            var matchingFiles = System.IO.Directory.GetFiles(uploadsDir, $"{emailClean}_*");
            if (matchingFiles.Length > 0)
            {
                return Ok(new ProviderApplicationStatusResponse
                {
                    Email = emailLower,
                    BusinessName = "Tourism Service Provider",
                    ServiceType = "Tourism Services",
                    Status = "Pending",
                    SubmittedAt = System.IO.File.GetCreationTimeUtc(matchingFiles[0]),
                    Message = "Your provider application has been received and is currently under review by our administration team."
                });
            }
        }

        return Ok(new { found = false, message = "No provider application was found for this email address. Please check the spelling or submit a new application." });
    }

    [HttpPost("status")]
    [HttpPost("/api/provider-applications/status")]
    public async Task<IActionResult> PostStatus([FromBody] ProviderApplicationStatusRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email address is required." });
        }

        return await GetStatus(request.Email);
    }
}
