using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Application.Abstractions.Persistence;

/// <summary>
/// Storage of one kind of entity. Every read is scoped to an owner, so a record of another user
/// is indistinguishable from one that does not exist (RNF2).
/// </summary>
public interface IOwnedRepository<TEntity>
    where TEntity : Entity
{
    Task<TEntity?> GetAsync(OwnerId owner, Guid id, CancellationToken cancellationToken = default);

    void Add(TEntity entity);
}
