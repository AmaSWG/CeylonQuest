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

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.WhenAll(
        RunCycles("booking expiration", (service, token) => service.ExpireAsync(DateTime.UtcNow, token), stoppingToken),
        RunCycles("cancellation outbox", (service, token) => service.PublishPendingCancellationsAsync(token), stoppingToken),
        RunCycles("Stripe checkout expiration", (service, token) => service.CloseExpiredCheckoutsAsync(
            new Stripe.Checkout.SessionService(), DateTime.UtcNow, token), stoppingToken));

    // Independent loops in the existing worker: an unavailable broker or Stripe cannot
    // delay persistence of cancellations. Every iteration gets its own scoped DbContext.
    private async Task RunCycles(string operation,
        Func<PendingPaymentExpirationService, CancellationToken, Task> action, CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            do
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    await action(scope.ServiceProvider.GetRequiredService<PendingPaymentExpirationService>(), token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed {Operation} cycle; will retry.", operation);
                }
            } while (await timer.WaitForNextTickAsync(token));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }
}
