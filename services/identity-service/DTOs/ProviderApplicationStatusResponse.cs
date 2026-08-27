using System;

namespace IdentityService.DTOs;

public class ProviderApplicationStatusRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ProviderApplicationStatusResponse
{
    public string Email { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Pending", "Approved", "Rejected"
    public string? RejectionReason { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
