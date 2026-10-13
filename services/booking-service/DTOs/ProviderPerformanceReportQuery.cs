using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class ProviderPerformanceReportQuery : IValidatableObject
{
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    [RegularExpression(
        "(?i)^(Experience|Restaurant|Accommodation)$",
        ErrorMessage = "Category must be Experience, Restaurant, or Accommodation.")]
    public string? Category { get; set; }

    [Range(0d, 5d)]
    public double? MinimumRating { get; set; }

    public Guid? ProviderId { get; set; }

    [Required]
    [RegularExpression(
        "(?i)^(Revenue|AverageRating)$",
        ErrorMessage = "SortBy must be Revenue or AverageRating.")]
    public string SortBy { get; set; } = "Revenue";

    [Required]
    [RegularExpression(
        "(?i)^(Asc|Desc)$",
        ErrorMessage = "SortDirection must be Asc or Desc.")]
    public string SortDirection { get; set; } = "Desc";

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (StartDate.HasValue &&
            EndDate.HasValue &&
            StartDate.Value > EndDate.Value)
        {
            yield return new ValidationResult(
                "StartDate must be on or before EndDate.",
                new[] { nameof(StartDate), nameof(EndDate) });
        }

        if (ProviderId == Guid.Empty)
        {
            yield return new ValidationResult(
                "ProviderId must be a non-empty GUID.",
                new[] { nameof(ProviderId) });
        }

        if (MinimumRating.HasValue &&
            !double.IsFinite(MinimumRating.Value))
        {
            yield return new ValidationResult(
                "MinimumRating must be a finite number.",
                new[] { nameof(MinimumRating) });
        }
    }
}