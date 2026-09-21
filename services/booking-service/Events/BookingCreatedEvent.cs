namespace BookingService.Events;

public class BookingCreatedEvent
{
    public Guid BookingId { get; set; }

    public Guid VisitorId { get; set; }

    public Guid ListingId { get; set; }

    public string BookingDate { get; set; } = string.Empty;

    public string TimeSlot { get; set; } = string.Empty;

    public int ParticipantCount { get; set; }

    public decimal TotalAmount { get; set; }
}