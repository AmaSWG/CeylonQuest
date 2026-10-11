namespace NotificationService.Models;

public class BookingContext
{
    public Guid BookingId { get; set; }
    public Guid VisitorId { get; set; }
    public Guid ProviderUserId { get; set; }
}
