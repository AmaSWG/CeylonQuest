using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProviderCatalogService.Events;
using Shared.Kafka;

namespace ProviderCatalogService.Services;

public class BookingCreatedConsumer : KafkaConsumerBase
{
    private const string BookingCreatedTopic = "booking.created";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<BookingCreatedConsumer> _logger;

    public BookingCreatedConsumer(
        IOptions<KafkaSettings> options,
        ILogger<BookingCreatedConsumer> logger)
        : base(options, logger)
    {
        _logger = logger;
    }

    protected override string GroupId => "provider-catalog-service";

    protected override IReadOnlyList<string> Topics =>
        new[] { BookingCreatedTopic };

    /// <summary>
    /// Handles booking.created events.
    ///
    /// Capacity is NOT deducted here because capacity is now reserved
    /// synchronously through the Provider Catalog reserve endpoint
    /// before the booking is created.
    ///
    /// This consumer keeps listening to booking.created events for
    /// logging and future event-driven processing without causing
    /// a second capacity deduction.
    /// </summary>
    protected override Task HandleMessageAsync(
        string topic,
        string? key,
        string value,
        CancellationToken cancellationToken)
    {
        BookingCreatedEvent? evt;

        try
        {
            evt = JsonSerializer.Deserialize<BookingCreatedEvent>(
                value,
                JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to deserialize {Topic} message: {Value}",
                topic,
                value);

            return Task.CompletedTask;
        }

        if (evt == null ||
            evt.ListingId == Guid.Empty ||
            string.IsNullOrWhiteSpace(evt.BookingDate))
        {
            _logger.LogWarning(
                "Received invalid {Topic} event, ignoring: {Value}",
                topic,
                value);

            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Received {Topic} for Booking {BookingId}, Listing {ListingId}, Date {Date}, Slot {Slot}, Participants {Participants}. Capacity was already reserved before booking creation; no additional deduction is required.",
            topic,
            evt.BookingId,
            evt.ListingId,
            evt.BookingDate,
            evt.TimeSlot,
            evt.ParticipantCount);

        return Task.CompletedTask;
    }
}