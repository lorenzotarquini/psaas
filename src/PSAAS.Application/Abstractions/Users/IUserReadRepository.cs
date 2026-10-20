using PSAAS.Application.Contracts.Users;

namespace PSAAS.Application.Abstractions.Users;

public interface IUserReadRepository
{
    Task<IReadOnlyList<UserReadModel>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    Task<UserReadModel?> GetByIdAsync(string tenantId, Guid userId, CancellationToken cancellationToken = default);

    Task<UserReadModel?> GetByKeycloakIdAsync(string keycloakId, CancellationToken cancellationToken = default);
}
