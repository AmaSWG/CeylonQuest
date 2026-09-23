namespace BookingService.DTOs;

public class RestaurantReservationListResponse
{
    public Guid Id { get; set; }

    public Guid RestaurantId { get; set; }

    public string BookingType { get; set; } = "Restaurant Reservation";

    public string ServiceName { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public string Time { get; set; } = string.Empty;

    public int PartySize { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal PricePerPerson { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }
}