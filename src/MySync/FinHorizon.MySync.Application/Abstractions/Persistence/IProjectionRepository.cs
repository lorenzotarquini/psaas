using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Projection;

namespace FinHorizon.MySync.Application.Abstractions.Persistence;

/// <summary>One saved <see cref="ProjectionSettings"/> per user, missing until the user saves (UC14-R11).</summary>
public interface IProjectionRepository
{
    Task<ProjectionSettings?> GetAsync(OwnerId owner, CancellationToken cancellationToken = default);

    void Add(ProjectionSettings settings);

    void Remove(ProjectionSettings settings);
}
