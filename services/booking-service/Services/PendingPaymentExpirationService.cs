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
        var cutoff = utcNow - PaymentWindow;
        var expired = new List<IPayableBooking>();

        expired.AddRange(await _db.Bookings.Where(b => b.Status == BookingStatus.PendingPayment && b.PaymentStatus != PaymentStatus.Paid && b.CreatedAt <= cutoff).ToListAsync(cancellationToken));
        expired.AddRange(await _db.RestaurantReservations.Where(b => b.Status == ReservationStatus.PendingPayment && b.PaymentStatus != PaymentStatus.Paid && b.CreatedAt <= cutoff).ToListAsync(cancellationToken));
        expired.AddRange(await _db.AccommodationBookings.Where(b => b.Status == AccommodationBookingStatus.PendingPayment && b.PaymentStatus != PaymentStatus.Paid && b.CreatedAt <= cutoff).ToListAsync(cancellationToken));

        foreach (var booking in expired) MarkExpired(booking, utcNow);

        if (expired.Count > 0) await _db.SaveChangesAsync(cancellationToken);
        return expired.Count;
    }

    public static bool MarkExpired(IPayableBooking booking, DateTime utcNow)
    {
        if (booking.Status != BookingStatus.PendingPayment ||
            booking.PaymentStatus == PaymentStatus.Paid || utcNow < booking.CreatedAt + PaymentWindow)
            return false;
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
        return true;
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

    public async Task CloseExpiredCheckoutsAsync(Stripe.Checkout.SessionService checkout,
        DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var attempts = await _db.PaymentTransactions.Where(t =>
            t.CheckoutSessionId != null && t.CheckoutClosedAt == null && t.CheckoutDeadline <= utcNow)
            .Take(100).ToListAsync(cancellationToken);
        var failures = new List<Exception>();
        foreach (var attempt in attempts)
        {
            try
            {
                var session = await checkout.GetAsync(attempt.CheckoutSessionId, cancellationToken: cancellationToken);
                if (session.Status == "open")
                    await checkout.ExpireAsync(attempt.CheckoutSessionId, cancellationToken: cancellationToken);
                attempt.CheckoutClosedAt = utcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Stripe.StripeException ex)
            {
                // Keep this session queued, but let the rest of the batch close.
                failures.Add(ex);
            }
        }
        if (failures.Count > 0) throw new AggregateException("Some Stripe sessions could not be closed; will retry.", failures);
    }
}
