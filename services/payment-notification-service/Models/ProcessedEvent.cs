namespace PaymentNotificationService.Models;

public class ProcessedEvent
{
    public string EventKey { get; set; } = "";
    public string Topic { get; set; } = "";
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
