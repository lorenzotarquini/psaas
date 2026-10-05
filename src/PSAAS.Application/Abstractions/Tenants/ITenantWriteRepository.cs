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
}
