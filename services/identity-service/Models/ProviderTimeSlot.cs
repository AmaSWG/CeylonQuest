namespace IdentityService.Models;

public class ProviderTimeSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProviderId { get; set; }

    public string Date { get; set; } = string.Empty;

    public string StartTime { get; set; } = string.Empty;

    public string EndTime { get; set; } = string.Empty;

    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
