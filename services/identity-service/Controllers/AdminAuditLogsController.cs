using System.Net.Http.Json;
using IdentityService.Data;
using IdentityService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = "Admin")]
public class AdminAuditLogsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminAuditLogsController(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? actionType,
        [FromQuery] Guid? administratorId,
        [FromQuery] Guid? targetId,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        // Bound the amount retrieved from each service.
        if (page < 1 || page > 100 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                message = "page must be 1–100 and pageSize must be 1–100."
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

        actionType = string.IsNullOrWhiteSpace(actionType)
            ? null
            : actionType.Trim();

        if (actionType is not null &&
            actionType != "UserReactivated" &&
            actionType != "UserSuspended" &&
            actionType != "ListingRemoved")
        {
            return BadRequest(new
            {
                message = "Unsupported actionType."
            });
        }

        var skip = (page - 1) * pageSize;
        var take = skip + pageSize;

        var entries = new List<AdminAuditEntryResponse>();
        var totalCount = 0;

        if (actionType != "ListingRemoved")
        {
            var query = _db.AdminAuditLogs.AsNoTracking();

            if (actionType is not null)
                query = query.Where(a => a.ActionType == actionType);

            if (administratorId.HasValue)
                query = query.Where(a =>
                    a.AdministratorId == administratorId.Value);

            if (targetId.HasValue)
                query = query.Where(a => a.TargetId == targetId.Value);

            if (dateFrom.HasValue)
            {
                var fromUtc = dateFrom.Value.UtcDateTime;
                query = query.Where(a => a.TimestampUtc >= fromUtc);
            }

            if (dateTo.HasValue)
            {
                var toUtc = dateTo.Value.UtcDateTime;
                query = query.Where(a => a.TimestampUtc <= toUtc);
            }

            totalCount += await query.CountAsync(cancellationToken);

            entries.AddRange(await query
                .OrderByDescending(a => a.TimestampUtc)
                .ThenByDescending(a => a.Id)
                .Take(take)
                .Select(a => new AdminAuditEntryResponse
                {
                    Id = a.Id,
                    ActionType = a.ActionType,
                    TimestampUtc = a.TimestampUtc,
                    AdministratorId = a.AdministratorId,
                    TargetId = a.TargetId,
                    TargetType = a.TargetType,
                    PreviousStatus = a.PreviousStatus,
                    NewStatus = a.NewStatus
                })
                .ToListAsync(cancellationToken));
        }

        if (actionType is null || actionType == "ListingRemoved")
        {
            var parameters = new List<string> { $"take={take}" };

            if (administratorId.HasValue)
                parameters.Add($"administratorId={administratorId.Value}");

            if (targetId.HasValue)
                parameters.Add($"targetId={targetId.Value}");

            if (dateFrom.HasValue)
                parameters.Add("dateFrom=" +
                    Uri.EscapeDataString(dateFrom.Value.ToString("O")));

            if (dateTo.HasValue)
                parameters.Add("dateTo=" +
                    Uri.EscapeDataString(dateTo.Value.ToString("O")));

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/catalog/admin/audit-logs?" +
                string.Join("&", parameters));

            // Forward the authenticated administrator's bearer token.
            request.Headers.TryAddWithoutValidation(
                "Authorization",
                Request.Headers["Authorization"].ToString());

            try
            {
                var client = _httpClientFactory.CreateClient("AuditCatalog");

                using var response = await client.SendAsync(
                    request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(502, new
                    {
                        message = "Listing audit records could not be retrieved."
                    });
                }

                var catalog = await response.Content
                    .ReadFromJsonAsync<AuditSourceResponse>(
                        cancellationToken: cancellationToken);

                if (catalog is null)
                {
                    return StatusCode(502, new
                    {
                        message = "The catalog audit response was empty."
                    });
                }

                entries.AddRange(catalog.Items);
                totalCount += catalog.TotalCount;
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, new
                {
                    message = "The catalog service is unavailable."
                });
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(504, new
                {
                    message = "The catalog audit request timed out."
                });
            }
            catch (System.Text.Json.JsonException)
            {
                return StatusCode(502, new
                {
                    message = "The catalog audit response was invalid."
                });
            }
        }

        // Both services use descending timestamp/ID order before Take.
        // Merge before applying the requested page.
        var items = entries
            .OrderByDescending(a => a.TimestampUtc)
            .ThenByDescending(a => a.Id)
            .ThenBy(a => a.ActionType)
            .Skip(skip)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.ActionType,
                TimestampUtc = DateTime.SpecifyKind(
                    a.TimestampUtc, DateTimeKind.Utc),
                a.AdministratorId,
                a.TargetId,
                a.TargetType,
                a.PreviousStatus,
                a.NewStatus,
                a.Reason
            })
            .ToList();

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }
}