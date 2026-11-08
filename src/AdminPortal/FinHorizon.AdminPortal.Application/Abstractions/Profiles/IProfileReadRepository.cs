using FinHorizon.AdminPortal.Application.Contracts.Profiles;
using FinHorizon.AdminPortal.Application.Contracts.Users;

namespace FinHorizon.AdminPortal.Application.Abstractions.Profiles;

public interface IProfileReadRepository
{
    Task<IReadOnlyList<ProfileReadModel>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>The profiles among <paramref name="profileIds"/> that exist, each with its tenant; unknown ids are skipped.</summary>
    Task<IReadOnlyList<UserProfileReadModel>> GetWithTenantAsync(
        IReadOnlyCollection<string> profileIds,
        CancellationToken cancellationToken = default);
}
