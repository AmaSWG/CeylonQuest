namespace BookingService.Services;

public class PendingPaymentExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingPaymentExpirationWorker> _logger;

    public PendingPaymentExpirationWorker(IServiceScopeFactory scopeFactory, ILogger<PendingPaymentExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<PendingPaymentExpirationService>();
                await service.ExpireAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pending-payment expiration cycle failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
