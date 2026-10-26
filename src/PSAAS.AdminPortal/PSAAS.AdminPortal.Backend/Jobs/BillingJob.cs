using NLog;
using PSAAS.Application.Services.Billing;

namespace PSAAS.AdminPortal.Backend.Jobs;

/// <summary>
/// UC23/UC24: the daily billing (renewals, trial ends, overdue invoices, reminders, suspensions) every
/// Billing:RunHours hours (0 or missing = off). Repeating it is harmless (UC23-R6).
/// </summary>
public sealed class BillingJob(IServiceScopeFactory scopes, IConfiguration configuration) : BackgroundService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hours = configuration.GetValue<int>("Billing:RunHours");
        if (hours <= 0)
        {
            Logger.Info("Billing job disabled");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(hours));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<BillingService>().RunAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Logger.Error(exception, "Billing job failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
