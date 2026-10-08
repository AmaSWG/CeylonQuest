using System.ComponentModel.DataAnnotations;

namespace BookingService.Models;

public class RestaurantReservation : IPayableBooking
{
    public Guid? ProviderId { get; set; }
    public DateTime? ScheduledEndAtUtc { get; set; }
    string IPayableBooking.BookingType => "Restaurant";
    Guid IPayableBooking.ListingId => RestaurantId;
    string IPayableBooking.ListingTitle => RestaurantName;
    DateOnly IPayableBooking.BookingDate => ReservationDate;
    int IPayableBooking.ParticipantCount => PartySize;
    decimal IPayableBooking.UnitPrice => PricePerPerson;
    decimal IPayableBooking.TotalAmount => TotalPrice;
    BookingStatus IPayableBooking.Status
    {
        get => Enum.Parse<BookingStatus>(Status.ToString());
        set => Status = Enum.Parse<ReservationStatus>(value.ToString());
    }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    [MaxLength(200)]
    public string? PaymentReference { get; set; }
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid VisitorId { get; set; }

    [Required]
    public Guid RestaurantId { get; set; }

    [Required]
    [MaxLength(200)]
    public string RestaurantName { get; set; } = string.Empty;

    [Required]
    public DateOnly ReservationDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string TimeSlot { get; set; } = string.Empty;

    [Required]
    public int PartySize { get; set; }

    [Required]
    public decimal PricePerPerson { get; set; }

    [Required]
    public decimal TotalPrice { get; set; }

    [Required]
    public ReservationStatus Status { get; set; } =
        ReservationStatus.PendingPayment;

    // =========================================================
    // Cancellation information
    // =========================================================

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    // =========================================================
    // Refund information
    // =========================================================

    public decimal RefundPercentage { get; set; } = 0m;

    public decimal RefundAmount { get; set; } = 0m;

    public DateTime? RefundedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }
}
