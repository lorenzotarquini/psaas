using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Tenants;
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

    /// <summary>UC11: the tenant (other than the excluded one) that already uses a normalized unique value.</summary>
    /// <param name="field">companyName, completeVatNumber or certifiedEmail.</param>
    Task<TenantReference?> FindByUniqueValueAsync(
        string field,
        string normalizedValue,
        Guid? excludedTenantId,
        CancellationToken cancellationToken = default);

    Task<TenantStructureReadModel> GetProfilesAndProductiveUnitsAsync(string tenantId, CancellationToken cancellationToken = default);
}
