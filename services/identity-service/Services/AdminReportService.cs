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
        var providerName = _db.Database.ProviderName ?? string.Empty;
        var isInMemory = providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase);

        var hasRoleFilter = !string.IsNullOrWhiteSpace(filters.Role);
        var hasStatusFilter = !string.IsNullOrWhiteSpace(filters.ApplicationStatus);

        RegistrationSummary? registrations = null;
        ApplicationSummary? applications = null;

        if (!hasStatusFilter || hasRoleFilter)
        {
            if (isInMemory)
            {
                registrations = await GetRegistrationSummaryLinqAsync(filters);
            }
            else
            {
                registrations = await GetRegistrationSummaryAdoAsync(filters);
            }
        }

        if (!hasRoleFilter || hasStatusFilter || string.Equals(filters.Role, "Provider", StringComparison.OrdinalIgnoreCase))
        {
            applications = await GetApplicationSummaryAsync(filters);
        }

        return new AdminReportResponse
        {
            AppliedFilters = filters,
            Registrations  = registrations,
            Applications   = applications,
            GeneratedAt    = DateTime.UtcNow
        };
    }

    private async Task<RegistrationSummary> GetRegistrationSummaryAdoAsync(ReportQueryParams f)
    {
        var conn = _db.Database.GetDbConnection();
        await EnsureOpenAsync(conn);

        var summary = new RegistrationSummary();

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
            parameters.Add(("@dateTo", f.DateTo.Value.Date.AddDays(1)));
        }

        if (!string.IsNullOrWhiteSpace(f.Role) &&
            Enum.TryParse<UserRole>(f.Role, true, out var parsedRole))
        {
            whereClauses.Add("Role = @role");
            parameters.Add(("@role", (int)parsedRole));
        }

        var where = whereClauses.Count > 0 ? " WHERE " + string.Join(" AND ", whereClauses) : string.Empty;

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

    private async Task<ApplicationSummary> GetApplicationSummaryAsync(ReportQueryParams f)
    {
        var summary = new ApplicationSummary();

        var query = _db.Users.Where(u => u.Role == UserRole.Provider);
        if (f.DateFrom.HasValue)
            query = query.Where(u => u.CreatedAt >= f.DateFrom.Value.Date);
        if (f.DateTo.HasValue)
            query = query.Where(u => u.CreatedAt < f.DateTo.Value.Date.AddDays(1));

        var approvedProviders = await query.ToListAsync();
        var approvedEmails = approvedProviders.Select(u => u.Email.ToLower()).ToHashSet();

        var uploadsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "uploads", "documents");
        var pendingApps = new List<(string ServiceType, DateTime SubmittedAt)>();

        if (System.IO.Directory.Exists(uploadsDir))
        {
            var jsonFiles = System.IO.Directory.GetFiles(uploadsDir, "*_application.json");
            foreach (var jf in jsonFiles)
            {
                try
                {
                    var text = await System.IO.File.ReadAllTextAsync(jf);
                    using var doc = System.Text.Json.JsonDocument.Parse(text);
                    var root = doc.RootElement;
                    var email = root.TryGetProperty("Email", out var e) ? e.GetString() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(email)) continue;
                    if (approvedEmails.Contains(email.Trim().ToLower())) continue;

                    var subAt = root.TryGetProperty("SubmittedAt", out var dt) ? dt.GetDateTime() : System.IO.File.GetCreationTimeUtc(jf);
                    if (f.DateFrom.HasValue && subAt < f.DateFrom.Value.Date) continue;
                    if (f.DateTo.HasValue && subAt >= f.DateTo.Value.Date.AddDays(1)) continue;

                    var sType = root.TryGetProperty("ServiceType", out var s) ? s.GetString() ?? "Tourism Services" : "Tourism Services";
                    pendingApps.Add((sType, subAt));
                }
                catch { }
            }
        }

        var statusFilter = f.ApplicationStatus?.Trim().ToLower();

        if (statusFilter == "pending")
        {
            summary.TotalApplications = pendingApps.Count;
            summary.PendingApplications = pendingApps.Count;
            summary.ApprovedApplications = 0;
            summary.RejectedApplications = 0;

            summary.ByServiceType = pendingApps
                .GroupBy(p => p.ServiceType)
                .Select(g => new ServiceTypeBreakdown { ServiceType = g.Key, Count = g.Count() })
                .OrderByDescending(b => b.Count)
                .ToList();
        }
        else if (statusFilter == "approved")
        {
            summary.TotalApplications = approvedProviders.Count;
            summary.PendingApplications = 0;
            summary.ApprovedApplications = approvedProviders.Count;
            summary.RejectedApplications = 0;

            summary.ByServiceType = new List<ServiceTypeBreakdown>
            {
                new() { ServiceType = "Tourism Services", Count = approvedProviders.Count }
            };
        }
        else if (statusFilter == "rejected")
        {
            summary.TotalApplications = 0;
            summary.PendingApplications = 0;
            summary.ApprovedApplications = 0;
            summary.RejectedApplications = 0;
            summary.ByServiceType = new List<ServiceTypeBreakdown>();
        }
        else
        {
            summary.PendingApplications = pendingApps.Count;
            summary.ApprovedApplications = approvedProviders.Count;
            summary.RejectedApplications = 0;
            summary.TotalApplications = pendingApps.Count + approvedProviders.Count;

            var allTypes = pendingApps.Select(p => p.ServiceType)
                .Concat(Enumerable.Repeat("Tourism Services", approvedProviders.Count));

            summary.ByServiceType = allTypes
                .GroupBy(t => t)
                .Select(g => new ServiceTypeBreakdown { ServiceType = g.Key, Count = g.Count() })
                .OrderByDescending(b => b.Count)
                .ToList();
        }

        return summary;
    }

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
