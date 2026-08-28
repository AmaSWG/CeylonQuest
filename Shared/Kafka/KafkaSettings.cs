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
	
	// e.g. "SaslSsl"
	public string SecurityProtocol { get; set; } = string.Empty;
	
	// e.g. "Plain"
	public string SaslMechanism { get; set; } = string.Empty;
	
	// Confluent API Key
	public string SaslUsername { get; set; } = string.Empty;
	
	// Confluent API Secret
	public string SaslPassword { get; set; } = string.Empty;
}
