using System.Text.Json;
using Microsoft.Extensions.Options;
using ProviderCatalogService.Events;
using Shared.Kafka;

namespace ProviderCatalogService.Services;

public class ReviewSubmittedConsumer(IOptions<KafkaSettings> options, ILogger<ReviewSubmittedConsumer> logger,
    IServiceScopeFactory scopes) : KafkaConsumerBase(options, logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    protected override string GroupId => "provider-catalog-service-review-ratings";
    protected override IReadOnlyList<string> Topics => new[] { "review.submitted" };

    protected override async Task HandleMessageAsync(string topic, string? key, string value, CancellationToken cancellationToken)
    {
        ReviewSubmittedEvent? evt;
        try { evt = JsonSerializer.Deserialize<ReviewSubmittedEvent>(value, JsonOptions); }
        catch (JsonException)
        {
            logger.LogWarning("Ignoring malformed review.submitted event.");
            return;
        }
        if (!ReviewRatingProjectionService.IsValid(evt))
        {
            logger.LogWarning("Ignoring invalid or unsupported review.submitted event {EventId}.", evt?.EventId);
            return;
        }
        using var scope = scopes.CreateScope();
        // Exceptions propagate: KafkaConsumerBase commits only after this succeeds.
        var updated = await scope.ServiceProvider.GetRequiredService<ReviewRatingProjectionService>().ApplyAsync(evt!, cancellationToken);
        if (!updated)
            logger.LogWarning("Retained contribution for review {ReviewId}; listing {ListingId} no longer exists.", evt!.ReviewId, evt.ListingId);
    }
}
