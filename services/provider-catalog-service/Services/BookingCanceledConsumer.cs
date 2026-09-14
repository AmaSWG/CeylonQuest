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

public class BookingCanceledConsumer : KafkaConsumerBase
{
    private const string BookingCanceledTopic = "booking.canceled";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingCanceledConsumer> _logger;

    public BookingCanceledConsumer(
        IOptions<KafkaSettings> options,
        ILogger<BookingCanceledConsumer> logger,
        IServiceScopeFactory scopeFactory)
        : base(options, logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override string GroupId => "provider-catalog-service-booking-canceled";

    protected override IReadOnlyList<string> Topics => new[] { BookingCanceledTopic };

    protected override async Task HandleMessageAsync(
        string topic,
        string? key,
        string value,
        CancellationToken cancellationToken)
    {
        BookingCanceledEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<BookingCanceledEvent>(value, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize {Topic} message: {Value}", topic, value);
            return;
        }

        if (evt == null || evt.ListingId == Guid.Empty || string.IsNullOrWhiteSpace(evt.BookingDate))
        {
            _logger.LogWarning("Received invalid {Topic} event, ignoring: {Value}", topic, value);
            return;
        }

        _logger.LogInformation(
            "Processing {Topic} for Listing {ListingId}, Date {Date}, Slot {Slot}, Guests {Guests}",
            topic, evt.ListingId, evt.BookingDate, evt.TimeSlot, evt.ParticipantCount);

        using var scope = _scopeFactory.CreateScope();
        var availabilityService = scope.ServiceProvider.GetRequiredService<AvailabilityService>();

        if (DateOnly.TryParse(evt.BookingDate, out var date))
        {
            await availabilityService.RestoreCapacityAsync(evt.ListingId, date, evt.TimeSlot, evt.ParticipantCount);
            _logger.LogInformation("Successfully restored capacity for Listing {ListingId}", evt.ListingId);
        }
    }
}