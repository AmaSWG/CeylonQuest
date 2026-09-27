namespace BookingService.DTOs;

public class UserBookingResponse
{
    public Guid Id { get; set; }

    public Guid ServiceId { get; set; }

    // Experience / Restaurant / Accommodation
    public string BookingType { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    // Main date used by the combined My Bookings table
    public DateOnly Date { get; set; }

    // Experience/Restaurant time slot.
    // Accommodation can display "Stay".
    public string Time { get; set; } = string.Empty;

    // Participants / Party Size / Guests
    public int PeopleCount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? PaymentStatus { get; set; }

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
    // Used by Experience, Restaurant and Accommodation
    // where applicable.
    // =========================================================

    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public decimal? RefundPercentage { get; set; }

    public decimal? RefundAmount { get; set; }

    public DateTime? RefundedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}