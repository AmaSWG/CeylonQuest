namespace IdentityService.Models;

/// <summary>
/// Represents an available time slot offered by a Provider.
/// </summary>
public class ProviderTimeSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Foreign key to the User who owns this time slot.</summary>
    public Guid ProviderId { get; set; }

    /// <summary>Date of the slot (ISO 8601 date-only string stored as a string for InMemory DB compat).</summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>Start time in HH:mm format, e.g. "09:00".</summary>
    public string StartTime { get; set; } = string.Empty;

    /// <summary>End time in HH:mm format, e.g. "11:00".</summary>
    public string EndTime { get; set; } = string.Empty;

    /// <summary>Whether the slot is still available for booking.</summary>
    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
