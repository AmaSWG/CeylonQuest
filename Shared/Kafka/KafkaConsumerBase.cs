using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shared.Kafka;

public abstract class KafkaConsumerBase : BackgroundService
{
    private readonly KafkaSettings _settings;
    private readonly ILogger _logger;

    protected KafkaConsumerBase(IOptions<KafkaSettings> options, ILogger logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    protected abstract string GroupId { get; }
    protected abstract IReadOnlyList<string> Topics { get; }

    protected abstract Task HandleMessageAsync(
        string topic,
        string? key,
        string value,
        CancellationToken cancellationToken);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
        };

        // Apply SASL/SSL only when configured
        if (!string.IsNullOrWhiteSpace(_settings.SecurityProtocol))
        {
            
            var normalizedProtocol = _settings.SecurityProtocol.Replace("_", "").Replace("-", "");

            if (Enum.TryParse<SecurityProtocol>(normalizedProtocol, true, out var secProtocol))
            {
                config.SecurityProtocol = secProtocol;

                if (!string.IsNullOrWhiteSpace(_settings.SaslMechanism))
                {
                    var normalizedMechanism = _settings.SaslMechanism.Replace("_", "").Replace("-", "");
                    if (Enum.TryParse<SaslMechanism>(normalizedMechanism, true, out var saslMech))
                    {
                        config.SaslMechanism = saslMech;
                    }
                }

                config.SaslUsername = _settings.SaslUsername ?? _settings.ApiKey;
                config.SaslPassword = _settings.SaslPassword ?? _settings.ApiSecret;
            }
        }

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