using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.Services;

namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly InventoryReportService _reportService;
    private readonly CatalogDbContext _db;

    public ReportsController(InventoryReportService reportService, CatalogDbContext db)
    {
        _reportService = reportService;
        _db = db;
    }

    [HttpGet("inventory-summary")]
    public async Task<IActionResult> GetInventoryReport(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] string? category,
        [FromQuery] string? location,
        [FromQuery] string? status)
    {
        var start = DateOnly.TryParse(startDate, out var s) ? s : DateOnly.FromDateTime(DateTime.UtcNow);
        var end = DateOnly.TryParse(endDate, out var e) ? e : start.AddDays(7);

        Guid? providerId = null;

        // If logged-in user is a Provider, scope to their business only
        if (User.IsInRole("Provider") && !User.IsInRole("Admin"))
        {
            var identityUserId = GetIdentityUserId();
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

            var provider = await _db.Providers.FirstOrDefaultAsync(p =>
                (identityUserId.HasValue && p.IdentityUserId == identityUserId.Value) ||
                (!string.IsNullOrEmpty(email) && p.Email.ToLower() == email.ToLower()));

            if (provider == null)
                return StatusCode(403, new { message = "Provider profile not found." });

            providerId = provider.Id;
        }

        var report = await _reportService.GenerateReportAsync(
            providerId,
            start,
            end,
            category,
            location,
            status);

        return Ok(report);
    }

    private Guid? GetIdentityUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}