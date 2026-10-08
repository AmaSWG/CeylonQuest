using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class CreateReviewRequest : IValidatableObject
{
    public Guid BookingId { get; set; }
    [Required, RegularExpression("^(Experience|Restaurant|Accommodation)$")]
    public string BookingType { get; set; } = string.Empty;
    [Range(1, 5)]
    public int Rating { get; set; }
    [Required, StringLength(2000)]
    public string Comment { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (BookingId == Guid.Empty)
            yield return new ValidationResult("BookingId is required.", new[] { nameof(BookingId) });
        if (string.IsNullOrWhiteSpace(Comment))
            yield return new ValidationResult("A written review is required.", new[] { nameof(Comment) });
    }
}
