namespace IdentityService.Models;

public class AdminAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ActionType { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public Guid AdministratorId { get; set; }
    public Guid TargetId { get; set; }
    public string TargetType { get; set; } = "User";
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
}