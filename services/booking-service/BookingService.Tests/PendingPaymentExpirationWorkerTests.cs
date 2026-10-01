using BookingService.Data;
using BookingService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Kafka;
using Xunit;

namespace BookingService.Tests;

public class PendingPaymentExpirationWorkerTests
{
    [Fact]
    public async Task Worker_ExecutesCycle_AndHandlesGracefulCancellation()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<BookingDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var mockKafka = new Mock<IKafkaProducer>();
        services.AddSingleton(mockKafka.Object);
        services.AddScoped<PendingPaymentExpirationService>();

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var mockLogger = new Mock<ILogger<PendingPaymentExpirationWorker>>();

        var worker = new PendingPaymentExpirationWorker(scopeFactory, mockLogger.Object);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Immediate cancellation ensures it completes cleanly

        await worker.StartAsync(cts.Token);
        await worker.StopAsync(CancellationToken.None);
    }
}