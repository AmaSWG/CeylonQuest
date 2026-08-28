namespace Shared.Kafka;

public class KafkaSettings
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public int MessageTimeoutMs { get; set; } = 5000;
    public int RetryDelaySeconds { get; set; } = 5;

    // Confluent Cloud SASL/SSL Authentication
    public string? SecurityProtocol { get; set; }   // "SaslSsl" or "SASL_SSL"
    public string? SaslMechanism { get; set; }      // "Plain" or "PLAIN"
    public string? SaslUsername { get; set; }       // API Key
    public string? SaslPassword { get; set; }       // API Secret

    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
}