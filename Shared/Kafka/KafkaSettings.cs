namespace Shared.Kafka;

// Kafka connection settings bound from the "Kafka" section in appsettings.json.
public class KafkaSettings
{
    public const string SectionName = "Kafka";

    // Broker address list, e.g. "localhost:9092".
    public string BootstrapServers { get; set; } = "localhost:9092";

    // Producer message timeout in milliseconds.
    public int MessageTimeoutMs { get; set; } = 5000;

    // Delay before reconnecting when the consumer loop fails.
    public int RetryDelaySeconds { get; set; } = 5;
	
	// Confluent Cloud SASL/SSL Authentication
	public string? SecurityProtocol { get; set; }   // "SASL_SSL" for Confluent Cloud
    public string? SaslMechanism { get; set; }       // "PLAIN" for Confluent Cloud
    public string? SaslUsername { get; set; }        // API Key
    public string? SaslPassword { get; set; }        // API Secret
	
}
