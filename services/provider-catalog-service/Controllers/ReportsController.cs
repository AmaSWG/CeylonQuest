using System;
using System.Globalization;
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
[Authorize(Roles = "Provider,Admin")]
public class ReportsController : ControllerBase
{
    private readonly InventoryReportService _reportService;
    private readonly CatalogDbContext _db;

    public ReportsController(InventoryReportService reportService, CatalogDbContext db)
    {
        _reportService = reportService;
        _db = db;
    }

    /// <summary>
    /// Generates an inventory summary report for the requested date range,
    /// scoped to the authenticated provider unless the caller is an admin
    /// </summary>
    [HttpGet("inventory-summary")]
    public async Task<IActionResult> GetInventoryReport(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] string? category,
        [FromQuery] string? location,
        [FromQuery] string? status)
    {
        DateOnly start;
        DateOnly end;

        // Default the start date to today when not supplied
        if (!string.IsNullOrWhiteSpace(startDate))
        {
            if (!DateOnly.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start))
            {
                return BadRequest(new { message = "Invalid startDate format. Expected YYYY-MM-DD." });
            }
        }
        else
        {
            start = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        // Default the end date to one week after the start date
        if (!string.IsNullOrWhiteSpace(endDate))
        {
            if (!DateOnly.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out end))
            {
                return BadRequest(new { message = "Invalid endDate format. Expected YYYY-MM-DD." });
            }
        }
        else
        {
            end = start.AddDays(7);
        }

        if (end < start)
        {
            return BadRequest(new { message = "endDate must be greater than or equal to startDate." });
        }

        // Max window limit to protect performance (e.g. max 90 days)
        if (end.DayNumber - start.DayNumber > 90)
        {
            return BadRequest(new { message = "Date window cannot exceed 90 days." });
        }

        Guid? providerId = null;

        // Non-admin providers are restricted to their own inventory data
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

    /// <summary>
    /// Extracts the authenticated user's identity ID from the available
    /// NameIdentifier, subject, or nameid token claim
    /// </summary>
    private Guid? GetIdentityUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")
                   ?? User.FindFirstValue("nameid");

        return Guid.TryParse(idClaim, out var guid) ? guid : null;
    }
}