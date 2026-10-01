namespace ProviderCatalogService.Models;

public class ProviderApplication
{
    public Guid Id { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string ServiceType { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? LegalDocumentPath { get; set; }

    public string? LegalDocumentFileName { get; set; }

    public string LegalDocumentsJson { get; set; } = "[]";

    public ProviderStatus Status { get; set; } = ProviderStatus.Pending;

    public string? RejectionReason { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }
	
}