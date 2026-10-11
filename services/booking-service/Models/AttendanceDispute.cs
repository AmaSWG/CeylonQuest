namespace BookingService.Models;

public class AttendanceDispute
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public string BookingType { get; set; } = "";
    public Guid VisitorId { get; set; }
    public AttendanceStatus OriginalAttendanceStatus { get; set; }
    public Guid? OriginalVerifiedByUserId { get; set; }
    public DateTime? OriginalVerifiedAtUtc { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public string? Resolution { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public AttendanceStatus? ResolvedAttendanceStatus { get; set; }
}
