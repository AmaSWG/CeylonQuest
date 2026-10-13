using System.ComponentModel.DataAnnotations;

namespace ProviderCatalogService.DTOs;

public class RemoveListingRequest : IValidatableObject
{
    public bool ConfirmRemoval { get; set; }

    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!ConfirmRemoval)
        {
            yield return new ValidationResult(
                "Listing removal must be confirmed.",
                new[] { nameof(ConfirmRemoval) });
        }

        if (string.IsNullOrWhiteSpace(Reason))
        {
            yield return new ValidationResult(
                "A removal reason is required.",
                new[] { nameof(Reason) });
        }
    }
}