using FinHorizon.AdminPortal.Application.Contracts;
using FinHorizon.AdminPortal.Application.Contracts.Tenants;

namespace FinHorizon.AdminPortal.Application.Abstractions.Tenants;

/// <summary>UC21: the tenant list with its plan, modules and renewal, read in one query.</summary>
public interface ITenantListRepository
{
    /// <param name="now">For the renewal filter.</param>
    Task<PagedResult<TenantListItem>> GetListAsync(TenantListQuery query, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<TenantCounts> GetCountsAsync(DateTimeOffset now, int renewalDays, CancellationToken cancellationToken = default);
}
