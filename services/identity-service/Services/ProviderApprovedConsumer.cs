using System.Text.Json;
using IdentityService.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKafka = Shared.Kafka;

namespace IdentityService.Services;

public class ProviderApprovedConsumer : SharedKafka.KafkaConsumerBase
{
    private const string ProviderApprovedTopic = "provider.approved";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProviderApprovedConsumer> _logger;

    public ProviderApprovedConsumer(
        IOptions<SharedKafka.KafkaSettings> options,
        ILogger<ProviderApprovedConsumer> logger,
        IServiceScopeFactory scopeFactory)
        : base(options, logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override string GroupId => "identity-service";

    protected override IReadOnlyList<string> Topics => new[] { ProviderApprovedTopic };

    protected override async Task HandleMessageAsync(
        string topic,
        string? key,
        string value,
        CancellationToken cancellationToken)
    {
        ProviderApproved? approved;
        try
        {
            approved = JsonSerializer.Deserialize<ProviderApproved>(value, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize {Topic} message: {Value}", topic, value);
            return;
        }

        if (approved is null || string.IsNullOrWhiteSpace(approved.Email))
        {
            _logger.LogWarning("Received invalid {Topic} event, ignoring. Payload: {Value}", topic, value);
            return;
        }

        _logger.LogInformation(
            "Received {Topic} for application {ApplicationId}, email {Email}",
            topic, approved.ApplicationId, approved.Email);

        using var scope = _scopeFactory.CreateScope();
        var activationService = scope.ServiceProvider.GetRequiredService<ProviderAccountActivationService>();

        try
        {
            await activationService.ActivateApprovedProviderAsync(approved, cancellationToken);
            _logger.LogInformation(
                "Successfully activated Identity account for approved provider {Email} (application {ApplicationId})",
                approved.Email, approved.ApplicationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to activate Identity account for approved provider {Email} (application {ApplicationId})",
                approved.Email, approved.ApplicationId);
        }
    }
}
