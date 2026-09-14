using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProviderCatalogService.Events;
using Shared.Kafka;

namespace ProviderCatalogService.Services;

public class BookingUpdatedConsumer : KafkaConsumerBase
{
    private const string BookingUpdatedTopic = "booking.updated";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingUpdatedConsumer> _logger;

    public BookingUpdatedConsumer(
        IOptions<KafkaSettings> options,
        ILogger<BookingUpdatedConsumer> logger,
        IServiceScopeFactory scopeFactory)
        : base(options, logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override string GroupId => "provider-catalog-service-booking-updated";

    protected override IReadOnlyList<string> Topics => new[] { BookingUpdatedTopic };

    /// <summary>
    /// Handles a booking updated event by deserializing the payload and
    /// rebalancing capacity from the old slot to the new slot
    /// </summary>
    protected override async Task HandleMessageAsync(
        string topic,
        string? key,
        string value,
        CancellationToken cancellationToken)
    {
        BookingUpdatedEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<BookingUpdatedEvent>(value, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize {Topic} message: {Value}", topic, value);
            return;
        }

        // Ignore events missing the required listing identifier
        if (evt == null || evt.ListingId == Guid.Empty)
        {
            _logger.LogWarning("Received invalid {Topic} event, ignoring: {Value}", topic, value);
            return;
        }

        _logger.LogInformation(
            "Processing {Topic} for Listing {ListingId}: [{OldDate} {OldSlot} ({OldGuests})] -> [{NewDate} {NewSlot} ({NewGuests})]",
            topic, evt.ListingId, evt.OldBookingDate, evt.OldTimeSlot, evt.OldParticipantCount,
            evt.NewBookingDate, evt.NewTimeSlot, evt.NewParticipantCount);

        using var scope = _scopeFactory.CreateScope();
        var availabilityService = scope.ServiceProvider.GetRequiredService<AvailabilityService>();

        // Both the old and new dates must parse before rebalancing capacity
        if (DateOnly.TryParse(evt.OldBookingDate, out var oldDate) &&
            DateOnly.TryParse(evt.NewBookingDate, out var newDate))
        {
            await availabilityService.UpdateCapacityAsync(
                evt.ListingId,
                oldDate,
                evt.OldTimeSlot,
                evt.OldParticipantCount,
                newDate,
                evt.NewTimeSlot,
                evt.NewParticipantCount);

            _logger.LogInformation("Successfully updated availability for Listing {ListingId}", evt.ListingId);
        }
    }
}