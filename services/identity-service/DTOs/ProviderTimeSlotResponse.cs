namespace IdentityService.DTOs;

public class ProviderTimeSlotResponse
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
}
