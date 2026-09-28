using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class CreatePaymentRequest
{
    [Required]
    public Guid BookingId { get; set; }

    [RegularExpression("^(Experience|Accommodation|Restaurant)$")]
    public string BookingType { get; set; } = "Experience";
}
