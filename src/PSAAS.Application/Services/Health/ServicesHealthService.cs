using System.Diagnostics;
using NLog;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Abstractions.Geography;
using PSAAS.Application.Abstractions.Health;
using PSAAS.Application.Abstractions.Users;

namespace PSAAS.Application.Services.Health;

public sealed record ServiceHealth(string Key, string Status, long LatencyMs, string? Detail);

/// <param name="Status">The worst status among the configured services (UC26-R3).</param>
public sealed record ServicesHealth(string Status, DateTimeOffset CheckedAt, IReadOnlyList<ServiceHealth> Services);

/// <summary>UC26: checks every connected system, one at a time, each within <see cref="Timeout"/>; never fails as a whole.</summary>
public sealed class ServicesHealthService(IEnumerable<IServiceProbe> probes, TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static readonly string[] Severity = [ServiceStatuses.Up, ServiceStatuses.Degraded, ServiceStatuses.Down];

    public async Task<ServicesHealth> CheckAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ServiceHealth>();
        // Sequential: probes may share the scoped DbContext.
        foreach (var probe in probes)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(Timeout);
            var watch = Stopwatch.StartNew();
            ProbeResult result;
            try
            {
                result = await probe.CheckAsync(timeout.Token);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                // UC26 2a: a slow or failing system is Down with the reason; the others are still checked.
                Logger.Warn(exception, "Service {Service} check failed", probe.Key);
                result = new ProbeResult(ServiceStatuses.Down, exception is OperationCanceledException ? "Timeout" : exception.Message);
            }

            results.Add(new ServiceHealth(probe.Key, result.Status, watch.ElapsedMilliseconds, result.Detail));
        }

        var worst = results.Where(r => r.Status != ServiceStatuses.NotConfigured).Select(r => Array.IndexOf(Severity, r.Status)).DefaultIfEmpty(0).Max();
        return new ServicesHealth(Severity[worst], clock.GetUtcNow(), results);
    }
}

/// <summary>Keycloak admin API (UC6, UC7, UC19): a count query on a profile nobody has.</summary>
public sealed class UserDirectoryProbe(IUserDirectory users) : IServiceProbe
{
    public string Key => "keycloak-admin";

    public async Task<ProbeResult> CheckAsync(CancellationToken cancellationToken)
    {
        await users.CountByProfilesAsync(["__psaas_probe__"], cancellationToken);
        return new ProbeResult(ServiceStatuses.Up);
    }
}

/// <summary>Italian city list (UC10).</summary>
public sealed class CityListProbe(IItalianCityProvider cities) : IServiceProbe
{
    public string Key => "city-list";

    public async Task<ProbeResult> CheckAsync(CancellationToken cancellationToken) =>
        await cities.SearchAsync("Roma", cancellationToken) is null
            ? new ProbeResult(ServiceStatuses.Down, "Elenco non raggiungibile")
            : new ProbeResult(ServiceStatuses.Up);
}

/// <summary>Provisioning hooks of the modules (UC18): degraded while some module is failed.</summary>
public sealed class ModuleHooksProbe(ISubscriptionRepository subscriptions) : IServiceProbe
{
    public string Key => "module-hooks";

    public async Task<ProbeResult> CheckAsync(CancellationToken cancellationToken)
    {
        var failed = (await subscriptions.GetFailedAsync(int.MaxValue, cancellationToken)).Count;
        return failed == 0 ? new ProbeResult(ServiceStatuses.Up) : new ProbeResult(ServiceStatuses.Degraded, failed.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
