namespace IdentityService.Models;

/// <summary>
/// Represents a service / activity price entry offered by a Provider.
/// </summary>
public class ProviderServicePrice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Foreign key to the User (Provider) who owns this price entry.</summary>
    public Guid ProviderId { get; set; }

    /// <summary>Name of the service / activity, e.g. "Wildlife Safari", "Guided Trek".</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Optional description of the service.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Price per unit (in the booking currency, LKR).</summary>
    public decimal PricePerUnit { get; set; }

    /// <summary>Unit label, e.g. "per person", "per group", "per hour".</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Whether this activity/service offering is active.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
