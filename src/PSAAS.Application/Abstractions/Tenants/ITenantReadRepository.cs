using PSAAS.Domain;

namespace PSAAS.Application.Abstractions.Tenants;

public interface ITenantReadRepository
{
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
