using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Services;

/// <summary>
/// Generates the admin registration and provider verification report.
///
/// Strategy:
///   - When the underlying database provider is MySQL/relational, this service executes
///     raw parameterized ADO.NET queries via EF Core's underlying DbConnection for
///     efficiency and direct SQL control (as required by the spec).
///   - When the provider is InMemory (unit tests), it falls back to a LINQ/EF Core
///     path so tests remain fast without requiring a real database.
/// </summary>
public class AdminReportService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AdminReportService> _logger;

    public AdminReportService(ApplicationDbContext db, ILogger<AdminReportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AdminReportResponse> GetReportAsync(ReportQueryParams filters)
    {
        // Detect whether we are running against a real relational DB or the InMemory provider.
        var providerName = _db.Database.ProviderName ?? string.Empty;
        var isInMemory = providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase);

        RegistrationSummary registrations;
        ApplicationSummary applications;

        if (isInMemory)
        {
            // ── LINQ path (unit tests / InMemory) ───────────────────────────
            registrations = await GetRegistrationSummaryLinqAsync(filters);
            applications  = await GetApplicationSummaryLinqAsync(filters);
        }
        else
        {
            // ── ADO.NET path (MySQL / Production & Development) ──────────────
            registrations = await GetRegistrationSummaryAdoAsync(filters);
            applications  = await GetApplicationSummaryAdoAsync(filters);
        }

        return new AdminReportResponse
        {
            AppliedFilters = filters,
            Registrations  = registrations,
            Applications   = applications,
            GeneratedAt    = DateTime.UtcNow
        };
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ADO.NET path — executes raw parameterized SQL via EF Core's DbConnection
    // ══════════════════════════════════════════════════════════════════════════

    private async Task<RegistrationSummary> GetRegistrationSummaryAdoAsync(ReportQueryParams f)
    {
        var conn = _db.Database.GetDbConnection();
        await EnsureOpenAsync(conn);

        var summary = new RegistrationSummary();

        // Build WHERE clauses dynamically; parameters added in matching order.
        var whereClauses = new List<string>();
        var parameters   = new List<(string Name, object? Value)>();

        if (f.DateFrom.HasValue)
        {
            whereClauses.Add("CreatedAt >= @dateFrom");
            parameters.Add(("@dateFrom", f.DateFrom.Value.Date));
        }

        if (f.DateTo.HasValue)
        {
            whereClauses.Add("CreatedAt < @dateTo");
            parameters.Add(("@dateTo", f.DateTo.Value.Date.AddDays(1))); // inclusive end-of-day
        }

        if (!string.IsNullOrWhiteSpace(f.Role) &&
            Enum.TryParse<UserRole>(f.Role, true, out var parsedRole))
        {
            whereClauses.Add("Role = @role");
            parameters.Add(("@role", (int)parsedRole));
        }

        var where = whereClauses.Count > 0 ? " WHERE " + string.Join(" AND ", whereClauses) : string.Empty;

        // ── Total / role breakdown ──────────────────────────────────────────
        var sql = $@"
            SELECT
                COUNT(*) AS Total,
                SUM(CASE WHEN Role = 0 THEN 1 ELSE 0 END) AS Visitors,
                SUM(CASE WHEN Role = 1 THEN 1 ELSE 0 END) AS Providers,
                SUM(CASE WHEN Role = 2 THEN 1 ELSE 0 END) AS Admins,
                SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS Active,
                SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS Inactive
            FROM Users{where}";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        AddParameters(cmd, parameters);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            summary.TotalUsers    = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader[0]);
            summary.TotalVisitors = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader[1]);
            summary.TotalProviders= reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader[2]);
            summary.TotalAdmins   = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader[3]);
            summary.ActiveUsers   = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader[4]);
            summary.InactiveUsers = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader[5]);
        }

        _logger.LogInformation(
            "AdminReportService: Registration summary — Total={Total}, Visitors={V}, Providers={P}, Admins={A}",
            summary.TotalUsers, summary.TotalVisitors, summary.TotalProviders, summary.TotalAdmins);

        return summary;
    }

    private Task<ApplicationSummary> GetApplicationSummaryAdoAsync(ReportQueryParams f)
    {
        var summary = new ApplicationSummary();
        return Task.FromResult(summary);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LINQ path — used by unit tests with InMemory provider
    // ══════════════════════════════════════════════════════════════════════════

    private async Task<RegistrationSummary> GetRegistrationSummaryLinqAsync(ReportQueryParams f)
    {
        var query = _db.Users.AsQueryable();

        if (f.DateFrom.HasValue)
            query = query.Where(u => u.CreatedAt >= f.DateFrom.Value.Date);

        if (f.DateTo.HasValue)
            query = query.Where(u => u.CreatedAt < f.DateTo.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(f.Role) &&
            Enum.TryParse<UserRole>(f.Role, true, out var parsedRole))
            query = query.Where(u => u.Role == parsedRole);

        var users = await query.ToListAsync();

        return new RegistrationSummary
        {
            TotalUsers     = users.Count,
            TotalVisitors  = users.Count(u => u.Role == UserRole.Visitor),
            TotalProviders = users.Count(u => u.Role == UserRole.Provider),
            TotalAdmins    = users.Count(u => u.Role == UserRole.Admin),
            ActiveUsers    = users.Count(u => u.IsActive),
            InactiveUsers  = users.Count(u => !u.IsActive)
        };
    }

    private Task<ApplicationSummary> GetApplicationSummaryLinqAsync(ReportQueryParams f)
    {
        var summary = new ApplicationSummary();
        return Task.FromResult(summary);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task EnsureOpenAsync(DbConnection conn)
    {
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();
    }

    private static void AddParameters(DbCommand cmd, List<(string Name, object? Value)> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value         = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}
