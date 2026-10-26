using PSAAS.Application.Contracts.Usage;

namespace PSAAS.Application.Abstractions.Usage;

/// <summary>Daily usage per tenant, module and metric (UC25).</summary>
public interface IUsageRepository
{
    /// <summary>Counters add to the day's value, gauges replace it (UC25-R1); all records in one transaction.</summary>
    Task AddAsync(IReadOnlyList<UsageRecord> records, CancellationToken cancellationToken = default);

    /// <summary>The days from <paramref name="from"/> on, oldest first.</summary>
    Task<IReadOnlyList<UsageRecord>> GetAsync(Guid tenantId, DateOnly from, CancellationToken cancellationToken = default);
}
