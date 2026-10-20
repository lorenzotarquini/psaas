using PSAAS.Application.Contracts.Profiles;

namespace PSAAS.Application.Abstractions.Profiles;

public interface IProfileReadRepository
{
    Task<IReadOnlyList<ProfileReadModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> GetExistingIdsAsync(
        IReadOnlyCollection<string> profileIds,
        CancellationToken cancellationToken = default);
}
