using Microsoft.EntityFrameworkCore;
using NLog;
using PSAAS.Application.Abstractions.Health;
using PSAAS.Application.Abstractions.Usage;
using PSAAS.Application.Contracts.Usage;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Usage;

/// <summary>One row per tenant, module, metric and day (UC25).</summary>
public sealed class UsageDailyDbModel
{
    public string TenantId { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public DateOnly Day { get; set; }
    public string Kind { get; set; } = UsageKinds.Counter;
    public decimal Value { get; set; }
}

public sealed class UsageRepository(PsaasDbContext dbContext) : IUsageRepository
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task AddAsync(IReadOnlyList<UsageRecord> records, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);
        var tenantIds = records.Select(r => r.TenantId.ToString()).Distinct().ToArray();
        var days = records.Select(r => r.Day).Distinct().ToArray();
        // The rows the batch may touch, loaded once; the batch then updates them in memory.
        var existing = await dbContext.UsageDaily
            .Where(u => tenantIds.Contains(u.TenantId) && days.Contains(u.Day))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var rows = existing.ToDictionary(u => (u.TenantId, u.ModuleId, u.Metric, u.Day));
        foreach (var record in records)
        {
            var key = (record.TenantId.ToString(), record.ModuleId, record.Metric, record.Day);
            if (!rows.TryGetValue(key, out var row))
            {
                row = new UsageDailyDbModel { TenantId = key.Item1, ModuleId = record.ModuleId, Metric = record.Metric, Day = record.Day, Kind = record.Kind };
                dbContext.UsageDaily.Add(row);
                rows[key] = row;
            }

            row.Kind = record.Kind;
            row.Value = record.Kind == UsageKinds.Gauge ? record.Value : row.Value + record.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Logger.Debug("Saved {RecordCount} usage records into {RowCount} daily rows", records.Count, rows.Count);
    }

    public async Task<IReadOnlyList<UsageRecord>> GetAsync(Guid tenantId, DateOnly from, CancellationToken cancellationToken = default)
    {
        var id = tenantId.ToString();
        return await dbContext.UsageDaily.AsNoTracking()
            .Where(u => u.TenantId == id && u.Day >= from)
            .OrderBy(u => u.Day)
            .Select(u => new UsageRecord(tenantId, u.ModuleId, u.Metric, u.Kind, u.Day, u.Value))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>UC26: the PSAAS database.</summary>
public sealed class DatabaseProbe(PsaasDbContext dbContext) : IServiceProbe
{
    public string Key => "database";

    public async Task<ProbeResult> CheckAsync(CancellationToken cancellationToken) =>
        await dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false)
            ? new ProbeResult(ServiceStatuses.Up)
            : new ProbeResult(ServiceStatuses.Down, "Connessione rifiutata");
}

/// <summary>UC26: invoices stay internal until an SDI or payment adapter replaces NoOpInvoiceTransmitter (EXP-08).</summary>
public sealed class InvoiceTransmissionProbe : IServiceProbe
{
    public string Key => "invoice-transmission";

    public Task<ProbeResult> CheckAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new ProbeResult(ServiceStatuses.NotConfigured));
}
