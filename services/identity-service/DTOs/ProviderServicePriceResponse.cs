namespace IdentityService.DTOs;

public class ProviderServicePriceResponse
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PricePerUnit { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}
