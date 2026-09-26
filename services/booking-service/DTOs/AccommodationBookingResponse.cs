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

    public DateTime CreatedAt { get; set; }
}