using Confluent.Kafka;
using Microsoft.Extensions.Options;
using NotificationService.Data;
using NotificationService.Models;
using Shared.Kafka;

namespace NotificationService.Services;

public class NotificationEventConsumer(IOptions<KafkaSettings> settings, IConfiguration configuration,
    IServiceScopeFactory scopes, ILogger<NotificationEventConsumer> logger) : BackgroundService
{
    public static readonly IReadOnlyList<string> Topics =
        new[] { "booking.created", "payment.completed", "booking.canceled", "review.submitted" };

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => RunAsync(stoppingToken), stoppingToken);

    private async Task RunAsync(CancellationToken ct)
    {
        var s = settings.Value;
        var config = new ConsumerConfig
        {
            BootstrapServers = s.BootstrapServers,
            GroupId = configuration["Notifications:ConsumerGroupId"] ?? "payment-notification-service",
            EnableAutoCommit = false, EnableAutoOffsetStore = false,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        if (!string.IsNullOrWhiteSpace(s.SecurityProtocol))
        {
            config.SecurityProtocol = Enum.Parse<SecurityProtocol>(s.SecurityProtocol.Replace("_", "").Replace("-", ""), true);
            if (!string.IsNullOrWhiteSpace(s.SaslMechanism))
                config.SaslMechanism = Enum.Parse<SaslMechanism>(s.SaslMechanism.Replace("_", "").Replace("-", ""), true);
            config.SaslUsername = s.SaslUsername ?? s.ApiKey;
            config.SaslPassword = s.SaslPassword ?? s.ApiSecret;
        }
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(config).Build();
                consumer.Subscribe(Topics);
                logger.LogInformation("Notification consumer subscribed to {Topics}", string.Join(", ", Topics));
                while (!ct.IsCancellationRequested)
                {
                    var record = consumer.Consume(ct);
                    if (record?.Message == null) continue;
                    var payload = record.Message.Value ?? "null";
                    try
                    {
                        await using var scope = scopes.CreateAsyncScope();
                        await scope.ServiceProvider.GetRequiredService<NotificationEventProcessor>()
                            .ProcessAsync(record.Topic, payload, ct);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed notification event {Topic}/{Partition}/{Offset}; offset remains uncommitted",
                            record.Topic, record.Partition.Value, record.Offset.Value);
                        await RecordFailureAsync(record.Topic, payload, ex, ct);
                        // Recreate the consumer from committed offsets. Never process a later
                        // record on this partition and commit past the failed record.
                        throw;
                    }
                    consumer.Commit(record);
                }
            }

            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            
            {
                logger.LogError(ex, "Notification consumer restarting after failure");
                try { await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, s.RetryDelaySeconds)), ct); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            }
        }
    }

    private async Task RecordFailureAsync(string topic, string payload, Exception error, CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            var key = NotificationEventProcessor.PayloadKey(topic, payload);
            var failure = await db.FailedEvents.FindAsync(new object[] { key }, ct);
            if (failure == null)
            {
                failure = new FailedEvent { EventKey = key, Topic = topic, Payload = payload };
                db.FailedEvents.Add(failure);
            }
            failure.Error = error.Message; failure.AttemptCount++;
            failure.LastAttemptAtUtc = DateTime.UtcNow; failure.Resolved = false;
            await db.SaveChangesAsync(ct);
        }

        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogError(ex, "Cannot persist failure record; Kafka offset remains uncommitted"); }
    }
}
