namespace BookingService.DTOs;

public class ProviderBookingResponse
{
    public Guid Id { get; set; }

    // Customer information
    public Guid CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerEmail { get; set; }

    // Service information
    public Guid ServiceId { get; set; }

    // Experience Booking / Restaurant Reservation / Accommodation Booking
    public string BookingType { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    // Main date used by provider booking table
    public DateOnly Date { get; set; }

    public string Time { get; set; } = string.Empty;

    // Participant count / party size / guest count
    public int PeopleCount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? PaymentStatus { get; set; }

    // Unit price where available
    public decimal? UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    // =========================================================
    // ACCOMMODATION DETAILS
    // =========================================================

    public DateOnly? CheckInDate { get; set; }

    public DateOnly? CheckOutDate { get; set; }

    public int? NumberOfNights { get; set; }

    // =========================================================
    // CANCELLATION / REFUND DETAILS
    // =========================================================

    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public decimal? RefundPercentage { get; set; }

    public decimal? RefundAmount { get; set; }

    public DateTime? RefundedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}