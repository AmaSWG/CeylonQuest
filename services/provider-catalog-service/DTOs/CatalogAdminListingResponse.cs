namespace ProviderCatalogService.DTOs;

public class CatalogAdminListingResponse
{
    public Guid Id { get; set; }
    public string BookingType { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
}