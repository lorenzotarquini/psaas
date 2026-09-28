using PSAAS.Domain;

namespace PSAAS.Application.Abstractions.Tenants;

public interface ITenantReadRepository
{
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);
}
