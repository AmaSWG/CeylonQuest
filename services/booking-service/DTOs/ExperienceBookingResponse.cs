namespace BookingService.DTOs;

public class ExperienceBookingResponse
{
    public Guid Id { get; set; }

    public Guid ListingId { get; set; }

    public string BookingType { get; set; } = "Experience Booking";

    public string ServiceName { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public string Time { get; set; } = string.Empty;

    public int ParticipantCount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }
}