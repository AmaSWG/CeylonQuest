using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Events;
using NotificationService.Models;

namespace NotificationService.Services;

public class NotificationEventProcessor(NotificationDbContext db, IRecipientResolver resolver)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static string PayloadKey(string topic, string payload) => topic + ":" +
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

    private static T Parse<T>(string payload) => JsonSerializer.Deserialize<T>(payload, JsonOptions)
        ?? throw new JsonException("Event payload is null.");
        
    private static string EventKey(string topic, Guid? eventId, string fallback)
    {
        Require(eventId != Guid.Empty, "EventId must not be an empty GUID when supplied.");
        return topic + ":" + (eventId?.ToString() ?? fallback);
    }

    private static string ReadEventKey(string topic, string payload)
    {
        switch (topic)
        {
            case "booking.created":
                var created = Parse<BookingCreatedEvent>(payload);
                return EventKey(topic, created.EventId, created.BookingId.ToString());
            case "payment.completed":
                var paid = Parse<PaymentCompletedEvent>(payload);
                return EventKey(topic, paid.EventId, paid.PaymentId.ToString());
            case "booking.canceled":
                var canceled = Parse<BookingCanceledEvent>(payload);
                return EventKey(topic, canceled.EventId, $"{canceled.BookingId}:{canceled.CanceledAt:O}");
            case "review.submitted":
                var review = Parse<ReviewSubmittedEvent>(payload);
                return EventKey(topic, review.EventId, review.ReviewId.ToString());
            default: throw new JsonException($"Unsupported topic {topic}.");
        }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new JsonException(message);
    }

    public async Task ProcessAsync(string topic, string payload, CancellationToken ct)
    {
        var key = ReadEventKey(topic, payload);
        // Replays must not depend on recipient lookup still being available.
        if (await db.ProcessedEvents.AnyAsync(x => x.EventKey == key, ct)) return;
        Guid booking;
        Guid? listing = null, payment = null, review = null;
        decimal? amount = null, refund = null;
        int? rating = null;
        string? currency = null, refundStatus = null;
        string title, message;
        DateTime occurred;
        Guid[] recipients;
        BookingContext? contextToAdd = null;
        switch (topic)
        {
            case "booking.created":
                var created = Parse<BookingCreatedEvent>(payload);
                Require(created.BookingId != Guid.Empty && created.VisitorId != Guid.Empty &&
                    created.ListingId != Guid.Empty && created.CreatedAt != default &&
                    !string.IsNullOrWhiteSpace(created.BookingDate), "Invalid booking.created event.");
                booking = created.BookingId; listing = created.ListingId; occurred = created.CreatedAt;
                var provider = await resolver.ProviderAsync(created.ListingId, created.ProviderUserId, ct);
                recipients = [created.VisitorId, provider];
                title = "Booking created";
                message = $"Booking {booking} for {created.ListingType} {listing} on {created.BookingDate} " +
                    $"at {created.TimeSlot}, for {created.ParticipantCount} participant(s), was created.";
                contextToAdd = new() { BookingId = booking, VisitorId = created.VisitorId, ProviderUserId = provider };
                break;
            case "payment.completed":
                var paid = Parse<PaymentCompletedEvent>(payload);
                Require(paid.PaymentId != Guid.Empty && paid.BookingId != Guid.Empty &&
                    paid.VisitorId != Guid.Empty && paid.Amount >= 0 && paid.CompletedAt != default,
                    "Invalid payment.completed event.");
                booking = paid.BookingId; payment = paid.PaymentId; amount = paid.Amount;
                currency = paid.Currency; occurred = paid.CompletedAt; recipients = [paid.VisitorId];
                title = "Payment completed";
                message = $"Payment {payment} of {paid.Amount.ToString("0.00", CultureInfo.InvariantCulture)} " +
                    $"{currency} for booking {booking} was completed. Reference: {paid.TransactionReference}.";
                break;
            case "booking.canceled":
                var canceled = Parse<BookingCanceledEvent>(payload);
                Require(canceled.BookingId != Guid.Empty && canceled.ListingId != Guid.Empty &&
                    canceled.CanceledAt != default && (canceled.RefundAmount is null or >= 0),
                    "Invalid booking.canceled event.");
                booking = canceled.BookingId; listing = canceled.ListingId; occurred = canceled.CanceledAt;
                var users = await resolver.BookingAsync(booking, canceled.ListingId,
                    canceled.VisitorId, canceled.ProviderUserId, ct);
                recipients = [users.VisitorId, users.ProviderUserId];
                refund = canceled.RefundAmount; refundStatus = canceled.RefundStatus; currency = canceled.Currency;
                title = "Booking canceled";
                message = $"Booking {booking} for {canceled.ListingType} {listing} on {canceled.BookingDate} " +
                    $"at {canceled.TimeSlot} was canceled. Reason: {canceled.Reason ?? "Not supplied"}.";
                if (refund.HasValue || refundStatus != null)
                    message += $" Refund: {refund?.ToString("0.00", CultureInfo.InvariantCulture)} {currency}; status: {refundStatus ?? "Not supplied"}.";
                break;
            case "review.submitted":
                var submitted = Parse<ReviewSubmittedEvent>(payload);
                Require(submitted.ReviewId != Guid.Empty && submitted.BookingId != Guid.Empty &&
                    submitted.ListingId != Guid.Empty && submitted.Rating is >= 1 and <= 5 &&
                    submitted.SubmittedAt != default, "Invalid review.submitted event.");
                booking = submitted.BookingId; listing = submitted.ListingId; review = submitted.ReviewId;
                occurred = submitted.SubmittedAt; rating = submitted.Rating;
                recipients = [await resolver.ProviderAsync(submitted.ListingId, submitted.ProviderUserId, ct)];
                title = "New review";
                message = $"Review {review} for listing {listing}, booking {booking}: {rating}/5 stars. {submitted.ReviewText}";
                break;
            default: throw new JsonException($"Unsupported topic {topic}.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (await db.ProcessedEvents.AnyAsync(x => x.EventKey == key, ct))
        {
            await transaction.CommitAsync(ct);
            return;
        }
        if (contextToAdd != null && !await db.BookingContexts.AnyAsync(x => x.BookingId == booking, ct))
            db.BookingContexts.Add(contextToAdd);
        foreach (var recipient in recipients.Distinct())
            db.Notifications.Add(new Notification
            {
                RecipientUserId = recipient, EventKey = key, EventType = topic,
                Title = title, Message = message, BookingId = booking, ListingId = listing,
                PaymentId = payment, ReviewId = review, Amount = amount, Currency = currency,
                Rating = rating, RefundAmount = refund, RefundStatus = refundStatus, OccurredAtUtc = occurred
            });
        db.ProcessedEvents.Add(new() { EventKey = key, Topic = topic });
        var failureKey = PayloadKey(topic, payload);
        var failure = await db.FailedEvents.FindAsync(new object[] { failureKey }, ct);
        if (failure != null) failure.Resolved = true;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
