using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinHorizon.MySync.Infrastructure.Persistence;

/// <summary>Every query starts from the rows of one owner (RNF2).</summary>
internal abstract class OwnedRepository<TEntity>(MySyncDbContext db) : IOwnedRepository<TEntity>
    where TEntity : Entity
{
    protected MySyncDbContext Db => db;

    public Task<TEntity?> GetAsync(OwnerId owner, Guid id, CancellationToken cancellationToken = default) =>
        Of(owner).SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public void Add(TEntity entity) => db.Set<TEntity>().Add(entity);

    protected IQueryable<TEntity> Of(OwnerId owner) => db.Set<TEntity>().Where(entity => entity.OwnerId == owner);
}
