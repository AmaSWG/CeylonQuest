using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class CreatePaymentRequest
{
    [Required]
    public Guid BookingId { get; set; }

    // true  = successful simulated payment
    // false = failed/cancelled/back payment
    public bool SimulateSuccess { get; set; }
}