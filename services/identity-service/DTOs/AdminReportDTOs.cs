using System;
using System.Collections.Generic;

namespace IdentityService.DTOs;

public class ReportQueryParams
{
    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public string? Role { get; set; }

    public string? ApplicationStatus { get; set; }
}

public class RegistrationSummary
{
    public int TotalUsers { get; set; }
    public int TotalVisitors { get; set; }
    public int TotalProviders { get; set; }
    public int TotalAdmins { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
}

public class ApplicationSummary
{
    public int TotalApplications { get; set; }
    public int PendingApplications { get; set; }
    public int ApprovedApplications { get; set; }
    public int RejectedApplications { get; set; }

    public List<ServiceTypeBreakdown> ByServiceType { get; set; } = new();
}

public class ServiceTypeBreakdown
{
    public string ServiceType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AdminReportResponse
{
    public ReportQueryParams AppliedFilters { get; set; } = new();

    public RegistrationSummary? Registrations { get; set; }

    public ApplicationSummary? Applications { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
