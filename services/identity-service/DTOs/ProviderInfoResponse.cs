namespace IdentityService.DTOs;

public class ProviderInfoResponse
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public string BusinessName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "Verified";

    public DateTime MemberSince { get; set; }
}
