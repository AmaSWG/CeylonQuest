using System.Text.Json;
using Confluent.Kafka;

namespace CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

/// <summary>
/// Publishes the exact JSON contracts booking-service emits (see
/// services/notification-service/Events/*.cs, which mirror services/booking-service/Events/*.cs)
/// directly to Kafka. This exercises notification-service's real consumer, database and REST API
/// end-to-end, independent of booking-service's own business rules, Stripe/webhook availability,
/// or the review feature (not yet built) - see README.md for the rationale.
/// </summary>
public sealed class KafkaEventPublisher : IAsyncDisposable
{
    private readonly IProducer<string, string> _producer =
        new ProducerBuilder<string, string>(new ProducerConfig { BootstrapServers = TestConfig.KafkaBootstrapServers }).Build();

    public async Task PublishAsync(string topic, string key, object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload);
        await _producer.ProduceAsync(topic, new Message<string, string> { Key = key, Value = json }, ct);
    }

    public Task PublishBookingCreatedAsync(Guid bookingId, Guid visitorId, Guid providerUserId, Guid listingId,
        string listingType, string providerBusinessName, string bookingDate, string timeSlot, int participantCount,
        decimal totalAmount, CancellationToken ct = default) =>
        PublishAsync("booking.created", bookingId.ToString(), new
        {
            BookingId = bookingId,
            VisitorId = visitorId,
            ListingId = listingId,
            ProviderUserId = providerUserId,
            ProviderBusinessName = providerBusinessName,
            ListingType = listingType,
            BookingDate = bookingDate,
            TimeSlot = timeSlot,
            ParticipantCount = participantCount,
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow
        }, ct);

    public Task PublishPaymentCompletedAsync(Guid bookingId, Guid visitorId, decimal amount, string transactionReference,
        string bookingType = "Experience", string currency = "LKR", CancellationToken ct = default) =>
        PublishAsync("payment.completed", bookingId.ToString(), new
        {
            PaymentId = Guid.NewGuid(),
            BookingId = bookingId,
            VisitorId = visitorId,
            Amount = amount,
            Currency = currency,
            BookingType = bookingType,
            TransactionReference = transactionReference,
            PaymentStatus = "Paid",
            BookingStatus = "Confirmed",
            CompletedAt = DateTime.UtcNow
        }, ct);

    public Task PublishBookingCanceledAsync(Guid bookingId, Guid listingId, Guid visitorId, Guid providerUserId,
        string listingType, string bookingDate, string timeSlot, string? reason, decimal? refundAmount,
        string? refundStatus, string currency = "LKR", CancellationToken ct = default) =>
        PublishAsync("booking.canceled", bookingId.ToString(), new
        {
            BookingId = bookingId,
            ListingId = listingId,
            VisitorId = visitorId,
            ProviderUserId = providerUserId,
            ListingType = listingType,
            BookingDate = bookingDate,
            TimeSlot = timeSlot,
            ParticipantCount = 1,
            Reason = reason,
            RefundAmount = refundAmount,
            RefundStatus = refundStatus,
            Currency = currency,
            CanceledAt = DateTime.UtcNow
        }, ct);

    public Task PublishReviewSubmittedAsync(Guid bookingId, Guid listingId, Guid providerUserId, int rating,
        string reviewText, CancellationToken ct = default) =>
        PublishAsync("review.submitted", Guid.NewGuid().ToString(), new
        {
            ReviewId = Guid.NewGuid(),
            BookingId = bookingId,
            ListingId = listingId,
            ProviderUserId = providerUserId,
            Rating = rating,
            ReviewText = reviewText,
            SubmittedAt = DateTime.UtcNow
        }, ct);

    public ValueTask DisposeAsync()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
        return ValueTask.CompletedTask;
    }
}
