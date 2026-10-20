using PSAAS.Application.Contracts;
using PSAAS.Domain;

namespace PSAAS.Application.Abstractions.Tenants;

public interface ITenantReadRepository
{
    Task<PagedResult<Tenant>> GetPageAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<TenantStructureReadModel> GetProfilesAndProductiveUnitsAsync(string tenantId, CancellationToken cancellationToken = default);
}
