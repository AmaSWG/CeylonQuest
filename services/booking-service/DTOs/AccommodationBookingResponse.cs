using BookingService.Models;

namespace BookingService.DTOs;

public class AccommodationBookingResponse
{
    public Guid Id { get; set; }

    public Guid AccommodationId { get; set; }

    public string AccommodationName { get; set; } = string.Empty;

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public int NumberOfNights { get; set; }

    public int GuestCount { get; set; }

    public decimal PricePerNight { get; set; }

    public decimal TotalPrice { get; set; }

    public AccommodationBookingStatus Status { get; set; }

    // Cancellation / Refund information
    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public decimal RefundPercentage { get; set; }

    public decimal RefundAmount { get; set; }

    public DateTime? RefundedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}