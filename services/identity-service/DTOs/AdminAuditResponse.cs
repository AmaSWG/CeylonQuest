namespace IdentityService.DTOs;

public class AdminAuditEntryResponse
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public Guid AdministratorId { get; set; }
    public Guid TargetId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class AuditSourceResponse
{
    public List<AdminAuditEntryResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
}