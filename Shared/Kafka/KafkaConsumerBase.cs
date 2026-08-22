using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shared.Kafka;

// Reusable Kafka consumer base class.
// Derived services subscribe to topics and handle messages.

public abstract class KafkaConsumerBase : BackgroundService
{
    private readonly KafkaSettings _settings;
    private readonly ILogger _logger;

    protected KafkaConsumerBase(IOptions<KafkaSettings> options, ILogger logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    // Kafka consumer group id for this service.
    protected abstract string GroupId { get; }

    // Topics this consumer should subscribe to.
    protected abstract IReadOnlyList<string> Topics { get; }

    // Handles a single consumed message.
    protected abstract Task HandleMessageAsync(
        string topic,
        string? key,
        string value,
        CancellationToken cancellationToken);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run on a background thread so host startup is not blocked by the consume loop.
        return Task.Run(() => ConsumeLoopAsync(stoppingToken), stoppingToken);
    }

    private async Task ConsumeLoopAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = string.IsNullOrWhiteSpace(_settings.BootstrapServers)
            ? "localhost:9092"
            : _settings.BootstrapServers;

        var retryDelaySeconds = _settings.RetryDelaySeconds > 0 ? _settings.RetryDelaySeconds : 5;

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(config).Build();
                consumer.Subscribe(Topics);

                _logger.LogInformation(
                    "Kafka consumer group {GroupId} listening for [{Topics}] on {BootstrapServers}",
                    GroupId,
                    string.Join(", ", Topics),
                    bootstrapServers);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(stoppingToken);
                        if (result?.Message?.Value is null)
                        {
                            continue;
                        }

                        await HandleMessageAsync(
                            result.Topic,
                            result.Message.Key,
                            result.Message.Value,
                            stoppingToken);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error in group {GroupId}", GroupId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Kafka consumer group {GroupId} failed. Retrying in {RetryDelaySeconds}s. Is Kafka running at {BootstrapServers}?",
                    GroupId,
                    retryDelaySeconds,
                    bootstrapServers);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
