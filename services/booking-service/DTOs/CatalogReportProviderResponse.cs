namespace BookingService.DTOs;

public class CatalogReportProviderResponse
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
}