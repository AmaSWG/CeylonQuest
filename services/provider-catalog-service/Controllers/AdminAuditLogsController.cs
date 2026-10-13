using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;

namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/admin/audit-logs")]
[Authorize(Roles = "Admin")]
public class AdminAuditLogsController : ControllerBase
{
    private readonly CatalogDbContext _db;

    public AdminAuditLogsController(CatalogDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? administratorId,
        [FromQuery] Guid? targetId,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (take < 1 || take > 10000)
        {
            return BadRequest(new
            {
                message = "take must be between 1 and 10000."
            });
        }

        if (dateFrom.HasValue && dateTo.HasValue &&
            dateFrom.Value > dateTo.Value)
        {
            return BadRequest(new
            {
                message = "dateFrom must be before or equal to dateTo."
            });
        }

        var query = _db.ListingRemovalAudits.AsNoTracking();

        if (administratorId.HasValue)
            query = query.Where(a =>
                a.AdministratorId == administratorId.Value);

        if (targetId.HasValue)
            query = query.Where(a => a.ListingId == targetId.Value);

        if (dateFrom.HasValue)
        {
            var fromUtc = dateFrom.Value.UtcDateTime;
            query = query.Where(a => a.RemovedAtUtc >= fromUtc);
        }

        if (dateTo.HasValue)
        {
            var toUtc = dateTo.Value.UtcDateTime;
            query = query.Where(a => a.RemovedAtUtc <= toUtc);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.RemovedAtUtc)
            .ThenByDescending(a => a.Id)
            .Take(take)
            .Select(a => new
            {
                a.Id,
                ActionType = "ListingRemoved",
                TimestampUtc = a.RemovedAtUtc,
                a.AdministratorId,
                TargetId = a.ListingId,
                TargetType = a.ListingType,
                a.PreviousStatus,
                a.NewStatus,
                a.Reason
            })
            .ToListAsync(cancellationToken);

        return Ok(new { items, totalCount });
    }
}