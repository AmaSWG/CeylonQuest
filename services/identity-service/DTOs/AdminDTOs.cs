using System;
using System.ComponentModel.DataAnnotations;
using IdentityService.Models;

namespace IdentityService.DTOs;

public class AdminStatsResponse
{
    public int TotalUsers { get; set; }
    public int TotalVisitors { get; set; }
    public int TotalProviders { get; set; }
    public int TotalAdmins { get; set; }
    public int PendingApplications { get; set; }
    public int ApprovedApplications { get; set; }
    public int RejectedApplications { get; set; }
    public int TotalServices { get; set; }
}

public class AdminUserResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateUserStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}

public class AdminProviderApplicationResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LegalDocumentFileName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RejectApplicationRequest
{
    public string? Reason { get; set; }
}
