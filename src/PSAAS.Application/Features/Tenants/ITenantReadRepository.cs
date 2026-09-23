using PSAAS.Domain;

namespace PSAAS.Application.Features.Tenants;

public interface ITenantReadRepository
{
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);
}
