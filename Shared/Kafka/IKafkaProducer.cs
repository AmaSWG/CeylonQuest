namespace Shared.Kafka;

// Reusable Kafka producer used by services to publish messages.

public interface IKafkaProducer
{
    // Publishes a raw string message to a topic.
    Task PublishAsync(string topic, string? key, string value, CancellationToken cancellationToken = default);

    // Serializes <paramref name="message"/> as JSON and publishes it to a topic.
    Task PublishAsync<T>(string topic, string? key, T message, CancellationToken cancellationToken = default);
}
