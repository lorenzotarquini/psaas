using System.Text.RegularExpressions;
using NLog;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Usage;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Catalog;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Contracts.Usage;

namespace PSAAS.Application.Services.Usage;

/// <summary>UC25 (EXP-09): usage sent by the modules, and the usage of a tenant against its plan.</summary>
public sealed partial class UsageService(
    IUsageRepository usage,
    ITenantReadRepository tenants,
    ISubscriptionRepository subscriptions,
    ICatalogRepository catalog,
    IUserDirectory users,
    TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const int MaxRecords = 500;
    public const int MaxAgeDays = 366;
    public const int ActiveUserDays = 30;

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]{1,59}$")]
    private static partial Regex MetricRegex();

    /// <returns>Errors per field ("records[i].field"); empty when everything was saved.</returns>
    public async Task<IReadOnlyDictionary<string, string>> ReportAsync(UsageReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        var errors = new Dictionary<string, string>();
        var rows = report.Records ?? [];
        if (rows.Count is 0 or > MaxRecords)
        {
            errors["records"] = TenantFieldValidationErrorCodes.InvalidValue;
            return errors;
        }

        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        // The active modules of each tenant in the batch, read once.
        var activeModules = new Dictionary<Guid, HashSet<string>>();
        var records = new List<UsageRecord>();
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var field = $"records[{i}]";
            HashSet<string>? modules = null;
            if (row.TenantId is not { } tenantId) errors[$"{field}.tenantId"] = TenantFieldValidationErrorCodes.Required;
            else if (!activeModules.TryGetValue(tenantId, out modules))
            {
                var subscription = await subscriptions.GetAsync(tenantId, cancellationToken);
                if (subscription is null && await tenants.GetByIdAsync(tenantId, cancellationToken) is null)
                {
                    errors[$"{field}.tenantId"] = TenantFieldValidationErrorCodes.InvalidValue;
                }
                else
                {
                    modules = subscription?.Modules.Where(m => m.Status == ProvisioningStatuses.Active).Select(m => m.ModuleId).ToHashSet() ?? [];
                    activeModules[tenantId] = modules;
                }
            }

            var moduleId = row.ModuleId?.Trim() ?? string.Empty;
            if (moduleId.Length == 0) errors[$"{field}.moduleId"] = TenantFieldValidationErrorCodes.Required;
            else if (modules is not null && !modules.Contains(moduleId)) errors[$"{field}.moduleId"] = TenantFieldValidationErrorCodes.InvalidValue;
            var metric = row.Metric?.Trim() ?? string.Empty;
            if (metric.Length == 0) errors[$"{field}.metric"] = TenantFieldValidationErrorCodes.Required;
            else if (!MetricRegex().IsMatch(metric)) errors[$"{field}.metric"] = TenantFieldValidationErrorCodes.InvalidFormat;
            if (row.Value is null) errors[$"{field}.value"] = TenantFieldValidationErrorCodes.Required;
            else if (row.Value < 0) errors[$"{field}.value"] = TenantFieldValidationErrorCodes.InvalidValue;
            var kind = string.IsNullOrWhiteSpace(row.Kind) ? UsageKinds.Counter : row.Kind.Trim().ToLowerInvariant();
            if (kind is not (UsageKinds.Counter or UsageKinds.Gauge)) errors[$"{field}.kind"] = TenantFieldValidationErrorCodes.InvalidValue;
            var day = DateOnly.FromDateTime((row.At ?? now).UtcDateTime);
            if (day > today || day < today.AddDays(-MaxAgeDays)) errors[$"{field}.at"] = TenantFieldValidationErrorCodes.InvalidValue;

            if (errors.Count == 0) records.Add(new UsageRecord(row.TenantId!.Value, moduleId, metric, kind, day, row.Value!.Value));
        }

        if (errors.Count > 0)
        {
            Logger.Info("Usage report of {RecordCount} records rejected: {ValidationErrors}", rows.Count, errors);
            return errors;
        }

        await usage.AddAsync(records, cancellationToken);
        Logger.Info("Saved {RecordCount} usage records", records.Count);
        return errors;
    }

    /// <returns>Null when the tenant does not exist.</returns>
    public async Task<TenantUsage?> GetTenantUsageAsync(Guid tenantId, int days, CancellationToken cancellationToken = default)
    {
        if (await tenants.GetByIdAsync(tenantId, cancellationToken) is null) return null;
        var structure = await tenants.GetProfilesAndProductiveUnitsAsync(tenantId.ToString(), cancellationToken);
        var subscription = await subscriptions.GetAsync(tenantId, cancellationToken);
        var (userCount, activeUsers) = await CountUsersAsync(structure.Profiles.Select(p => p.Id).ToHashSet(), cancellationToken);

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var names = (await catalog.GetModulesAsync(cancellationToken)).ToDictionary(m => m.Id, m => m.Name);
        var series = (await usage.GetAsync(tenantId, today.AddDays(1 - days), cancellationToken))
            .GroupBy(r => (r.ModuleId, r.Metric, r.Kind))
            .Select(g =>
            {
                var points = g.OrderBy(r => r.Day).Select(r => new UsagePoint(r.Day, r.Value)).ToArray();
                var total = g.Key.Kind == UsageKinds.Gauge ? points[^1].Value : points.Sum(p => p.Value);
                return new UsageSeries(g.Key.ModuleId, names.GetValueOrDefault(g.Key.ModuleId, g.Key.ModuleId), g.Key.Metric, g.Key.Kind, total, points);
            })
            .OrderBy(s => s.ModuleName).ThenBy(s => s.Metric)
            .ToArray();

        return new TenantUsage(
            days, userCount, subscription?.MaxUsers, activeUsers,
            structure.ProductiveUnits.Count, subscription?.MaxProductiveUnits, series);
    }

    // UC25-R4 and 2a: Keycloak down leaves both counts empty.
    private async Task<(int? Users, int? Active)> CountUsersAsync(HashSet<string> profileIds, CancellationToken cancellationToken)
    {
        if (profileIds.Count == 0) return (0, 0);
        try
        {
            // shortcut: reads the users of the realm (search cap) to find the tenant's; a profile-filtered query when realms grow.
            var tenantUsers = (await users.SearchAsync(null, cancellationToken)).Where(u => u.ProfileIds.Any(profileIds.Contains)).ToArray();
            var logins = await users.GetLastLoginsAsync(tenantUsers.Select(u => u.Id).ToArray(), cancellationToken);
            var since = clock.GetUtcNow().AddDays(-ActiveUserDays);
            return (tenantUsers.Length, logins.Values.Count(at => at >= since));
        }
        catch (UserDirectoryUnavailableException exception)
        {
            Logger.Warn(exception, "Usage shown without user counts: Keycloak unreachable");
            return (null, null);
        }
    }
}
