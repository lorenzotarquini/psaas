using PSAAS.Domain;

namespace PSAAS.Application.Abstractions.Tenants;

/// <summary>
/// Write-side persistence contract for tenants, kept separate from the read
/// repository so read and write concerns evolve independently.
/// </summary>
public interface ITenantWriteRepository
{
    /// <summary>
    /// Persists a new tenant.
    /// </summary>
    /// <param name="tenant">The tenant aggregate to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="TenantUniquenessConflictException">
    /// A concurrent tenant already occupies one of the unique values
    /// (normalized company name, complete VAT number, normalized certified email).
    /// </exception>
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new tenant and its productive units atomically.
    /// </summary>
    Task AddWithProductiveUnitsAsync(
        Tenant tenant,
        IReadOnlyCollection<TenantProductiveUnitCreateModel> productiveUnits,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new tenant, new organization profiles and productive units atomically.
    /// </summary>
    Task AddWithProfilesAndProductiveUnitsAsync(
        Tenant tenant,
        IReadOnlyCollection<TenantProfileCreateModel> profiles,
        IReadOnlyCollection<TenantProductiveUnitCreateModel> productiveUnits,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes for an existing tenant.
    /// </summary>
    /// <returns><see langword="true" /> when the tenant exists and was updated; otherwise <see langword="false" />.</returns>
    /// <exception cref="TenantUniquenessConflictException">
    /// A concurrent tenant already occupies one of the unique values
    /// (normalized company name, complete VAT number, normalized certified email).
    /// </exception>
    Task<bool> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a tenant together with its profiles and productive units in one transaction.
    /// </summary>
    /// <returns><see langword="false" /> when the tenant does not exist.</returns>
    Task<bool> UpdateWithStructureAsync(
        Tenant tenant,
        IReadOnlyCollection<TenantProfileEditModel> profiles,
        IReadOnlyCollection<TenantProductiveUnitEditModel> productiveUnits,
        CancellationToken cancellationToken = default);

    /// <summary>EXP-04: stores a status change.</summary>
    /// <returns><see langword="false" /> when the tenant does not exist.</returns>
    Task<bool> UpdateLifecycleAsync(Guid tenantId, TenantLifecycle lifecycle, CancellationToken cancellationToken = default);

    /// <summary>Hard delete with profiles and units; only for drafts never activated (UC14).</summary>
    /// <returns><see langword="false" /> when the tenant does not exist.</returns>
    Task<bool> DeleteAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
