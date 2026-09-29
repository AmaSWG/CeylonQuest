using BookingService.Data;
using BookingService.Events;
using BookingService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Kafka;

namespace BookingService.Services;

public class PendingPaymentExpirationService
{
    public static readonly TimeSpan PaymentWindow = TimeSpan.FromMinutes(15);

    private readonly BookingDbContext _db;
    private readonly IKafkaProducer _kafka;

    public PendingPaymentExpirationService(BookingDbContext db, IKafkaProducer kafka)
    {
        _db = db;
        _kafka = kafka;
    }

    public async Task<int> ExpireAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await PublishPendingCancellationsAsync(cancellationToken);
        var cutoff = utcNow - PaymentWindow;
        var expired = new List<IPayableBooking>();

        expired.AddRange(await _db.Bookings.Where(b => !b.IsDeleted && b.Status == BookingStatus.PendingPayment && b.PaymentStatus != PaymentStatus.Paid && b.CreatedAt <= cutoff).ToListAsync(cancellationToken));
        expired.AddRange(await _db.RestaurantReservations.Where(b => !b.IsDeleted && b.Status == ReservationStatus.PendingPayment && b.PaymentStatus != PaymentStatus.Paid && b.CreatedAt <= cutoff).ToListAsync(cancellationToken));
        expired.AddRange(await _db.AccommodationBookings.Where(b => !b.IsDeleted && b.Status == AccommodationBookingStatus.PendingPayment && b.PaymentStatus != PaymentStatus.Paid && b.CreatedAt <= cutoff).ToListAsync(cancellationToken));

        foreach (var booking in expired)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.PaymentStatus = PaymentStatus.Failed;
            booking.UpdatedAt = utcNow;
            switch (booking)
            {
                case Booking b:
                    b.CancelledAt = utcNow;
                    b.CancellationReason = "Payment window expired.";
                    break;
                case RestaurantReservation r:
                    r.CancelledAt = utcNow;
                    r.CancellationReason = "Payment window expired.";
                    break;
                case AccommodationBooking a:
                    a.CancelledAt = utcNow;
                    a.CancellationReason = "Payment window expired.";
                    break;
            }
        }

        if (expired.Count == 0) return 0;

        await _db.SaveChangesAsync(cancellationToken);

        await PublishPendingCancellationsAsync(cancellationToken);

        return expired.Count;
    }

    public async Task PublishPendingCancellationsAsync(CancellationToken cancellationToken = default)
    {
        var messages = await _db.BookingCancellationMessages
            .Where(m => m.PublishedAt == null).Take(100).ToListAsync(cancellationToken);
        foreach (var message in messages)
        {
            var evt = System.Text.Json.JsonSerializer.Deserialize<BookingCanceledEvent>(message.Payload)!;
            await _kafka.PublishAsync("booking.canceled", message.BookingId.ToString(), evt, cancellationToken);
            message.PublishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
