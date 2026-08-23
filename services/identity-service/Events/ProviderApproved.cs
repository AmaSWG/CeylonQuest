namespace IdentityService.Events;

/// <summary>
/// Event received from the provider.approved Kafka topic, published by Provider/Catalog Service.
/// </summary>
public class ProviderApproved
{
    public Guid ApplicationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; }
}
