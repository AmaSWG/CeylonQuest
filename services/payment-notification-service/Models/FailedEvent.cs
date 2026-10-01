namespace PaymentNotificationService.Models;

public class FailedEvent
{
    public string EventKey { get; set; } = "";
    public string Topic { get; set; } = "";
    public string Payload { get; set; } = "";
    public string Error { get; set; } = "";
    public int AttemptCount { get; set; }
    public DateTime LastAttemptAtUtc { get; set; }
    public bool Resolved { get; set; }
}
