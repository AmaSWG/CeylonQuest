using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Kafka;

// DI helpers for registering shared Kafka services.
public static class KafkaServiceCollectionExtensions
{
    // Binds Kafka settings from appsettings.json and registers the shared producer.
    public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.SectionName));
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        return services;
    }
}
