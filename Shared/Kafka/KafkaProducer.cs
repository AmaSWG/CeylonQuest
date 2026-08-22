using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shared.Kafka;

// Reusable Confluent.Kafka producer that reads settings from appsettings.json.
public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;
    private readonly string _bootstrapServers;

    public KafkaProducer(IOptions<KafkaSettings> options, ILogger<KafkaProducer> logger)
    {
        _logger = logger;

        var settings = options.Value;
        _bootstrapServers = string.IsNullOrWhiteSpace(settings.BootstrapServers)
            ? "localhost:9092"
            : settings.BootstrapServers;

        var config = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            MessageTimeoutMs = settings.MessageTimeoutMs > 0 ? settings.MessageTimeoutMs : 5000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger.LogInformation("Shared Kafka producer configured for {BootstrapServers}", _bootstrapServers);
    }

    public Task PublishAsync<T>(string topic, string? key, T message, CancellationToken cancellationToken = default)
    {
        var value = JsonSerializer.Serialize(message, JsonOptions);
        return PublishAsync(topic, key, value, cancellationToken);
    }

    public async Task PublishAsync(string topic, string? key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(value);

        var kafkaMessage = new Message<string, string>
        {
            Key = key ?? string.Empty,
            Value = value
        };

        var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);

        _logger.LogInformation(
            "Published message to topic {Topic} (partition {Partition}, offset {Offset})",
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
