using NLog;
using PSAAS.Application.Services.Catalog;

namespace PSAAS.AdminPortal.Backend.Jobs;

/// <summary>
/// UC18: retries the module provisionings that failed, every Provisioning:RetryMinutes (0 or missing = off),
/// up to SubscriptionService.MaxProvisioningAttempts per module; then they wait for a manual "Riprova".
/// </summary>
public sealed class ProvisioningRetryJob(IServiceScopeFactory scopes, IConfiguration configuration) : BackgroundService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = configuration.GetValue<int>("Provisioning:RetryMinutes");
        if (minutes <= 0)
        {
            Logger.Info("Provisioning retry job disabled");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var retried = await scope.ServiceProvider.GetRequiredService<SubscriptionService>().RetryFailedAsync(stoppingToken);
                if (retried > 0) Logger.Info("Retried {Count} failed module provisionings", retried);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Logger.Error(exception, "Provisioning retry job failed");
            }
        }
    }
}
