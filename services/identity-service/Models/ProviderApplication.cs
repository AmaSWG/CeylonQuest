namespace IdentityService.Models;

public class ProviderApplication
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    // Business details
    public string BusinessName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Legal document filename (file upload handling is out of scope)
    public string? LegalDocumentFileName { get; set; }

    public ProviderApplicationStatus Status { get; set; } = ProviderApplicationStatus.Pending;

    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ProviderApplicationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
