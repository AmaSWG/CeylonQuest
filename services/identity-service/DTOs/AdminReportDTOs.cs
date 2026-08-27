using System;
using System.Collections.Generic;

namespace IdentityService.DTOs;

// ── Query Parameters ──────────────────────────────────────────────────────────

/// <summary>
/// Filter parameters for the admin registration and verification report.
/// All fields are optional; omitting a field means "no filter on that dimension."
/// </summary>
public class ReportQueryParams
{
    /// <summary>Inclusive lower bound for record CreatedAt (UTC date).</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>Inclusive upper bound for record CreatedAt (UTC date, end of day).</summary>
    public DateTime? DateTo { get; set; }

    /// <summary>Filter users by role: Visitor | Provider | Admin. Null = all roles.</summary>
    public string? Role { get; set; }

    /// <summary>Filter provider applications by status: Pending | Approved | Rejected. Null = all statuses.</summary>
    public string? ApplicationStatus { get; set; }
}

// ── Registration Summary ──────────────────────────────────────────────────────

/// <summary>Aggregated user registration counts, optionally filtered.</summary>
public class RegistrationSummary
{
    public int TotalUsers { get; set; }
    public int TotalVisitors { get; set; }
    public int TotalProviders { get; set; }
    public int TotalAdmins { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
}

// ── Application Summary ───────────────────────────────────────────────────────

/// <summary>Aggregated provider application counts, optionally filtered.</summary>
public class ApplicationSummary
{
    public int TotalApplications { get; set; }
    public int PendingApplications { get; set; }
    public int ApprovedApplications { get; set; }
    public int RejectedApplications { get; set; }

    /// <summary>Breakdown of application counts grouped by ServiceType.</summary>
    public List<ServiceTypeBreakdown> ByServiceType { get; set; } = new();
}

/// <summary>Application count for a specific service type.</summary>
public class ServiceTypeBreakdown
{
    public string ServiceType { get; set; } = string.Empty;
    public int Count { get; set; }
}

// ── Report Response ───────────────────────────────────────────────────────────

/// <summary>Full report response returned by GET /api/admin/reports.</summary>
public class AdminReportResponse
{
    /// <summary>The filters that were applied to generate this report.</summary>
    public ReportQueryParams AppliedFilters { get; set; } = new();

    /// <summary>Registration / user account aggregation.</summary>
    public RegistrationSummary Registrations { get; set; } = new();

    /// <summary>Provider application verification aggregation.</summary>
    public ApplicationSummary Applications { get; set; } = new();

    /// <summary>UTC timestamp when this report was generated.</summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
