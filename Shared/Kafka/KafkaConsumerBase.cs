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

        // Parse SecurityProtocol robustly without breaking enum name formats (e.g. "SaslSsl", "SASL_SSL")
        if (!string.IsNullOrWhiteSpace(_settings.SecurityProtocol))
        {
            var rawProtocol = _settings.SecurityProtocol.Replace("-", "_");

            if (Enum.TryParse<SecurityProtocol>(rawProtocol, true, out var secProtocol) ||
                Enum.TryParse<SecurityProtocol>(_settings.SecurityProtocol.Replace("_", ""), true, out secProtocol))
            {
                config.SecurityProtocol = secProtocol;

                if (!string.IsNullOrWhiteSpace(_settings.SaslMechanism))
                {
                    var rawMechanism = _settings.SaslMechanism.Replace("-", "_");
                    if (Enum.TryParse<SaslMechanism>(rawMechanism, true, out var saslMech) ||
                        Enum.TryParse<SaslMechanism>(_settings.SaslMechanism.Replace("_", ""), true, out saslMech))
                    {
                        config.SaslMechanism = saslMech;
                    }
                }

                config.SaslUsername = _settings.SaslUsername ?? _settings.ApiKey;
                config.SaslPassword = _settings.SaslPassword ?? _settings.ApiSecret;
            }
            else
            {
                _logger.LogWarning("Failed to parse SecurityProtocol '{SecurityProtocol}'. Defaulting to Plaintext.", _settings.SecurityProtocol);
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(config)
                    .SetErrorHandler((_, error) =>
                    {
                        _logger.LogError("Kafka Consumer Error [{Code}]: {Reason} (IsFatal: {IsFatal})", 
                            error.Code, error.Reason, error.IsFatal);
                    })
                    .Build();

                consumer.Subscribe(Topics);

                _logger.LogInformation(
                    "Kafka consumer group {GroupId} listening for [{Topics}] on {BootstrapServers} (SecurityProtocol: {SecurityProtocol})",
                    GroupId,
                    string.Join(", ", Topics),
                    bootstrapServers,
                    config.SecurityProtocol?.ToString() ?? "Plaintext");

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