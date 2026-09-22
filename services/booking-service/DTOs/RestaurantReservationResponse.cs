using BookingService.Models;

namespace BookingService.DTOs;

public class RestaurantReservationResponse
{
    public Guid Id { get; set; }

    public Guid RestaurantId { get; set; }

    public string RestaurantName { get; set; } = string.Empty;

    public DateOnly ReservationDate { get; set; }

    public string TimeSlot { get; set; } = string.Empty;

    public int PartySize { get; set; }

    public ReservationStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}