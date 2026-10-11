namespace BookingService.Models;

// Common payment view over the three existing booking entities; not an EF entity.
public interface IPayableBooking
{
    AttendanceStatus AttendanceStatus { get; set; }
    Guid? AttendanceVerifiedByUserId { get; set; }
    DateTime? AttendanceVerifiedAtUtc { get; set; }
    Guid Id { get; }
    Guid VisitorId { get; }
    Guid? ProviderUserId { get; }
    string BookingType { get; }
    Guid ListingId { get; }
    string ListingTitle { get; }
    DateOnly BookingDate { get; }
    string TimeSlot { get; }
    int ParticipantCount { get; }
    decimal UnitPrice { get; }
    decimal TotalAmount { get; }
    BookingStatus Status { get; set; }
    PaymentStatus PaymentStatus { get; set; }
    string? PaymentReference { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
