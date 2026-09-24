namespace BookingService.DTOs;

public class ProviderBookingResponse
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid ServiceId { get; set; }

    public string BookingType { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public string Time { get; set; } = string.Empty;

    public int PeopleCount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? PaymentStatus { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }
}