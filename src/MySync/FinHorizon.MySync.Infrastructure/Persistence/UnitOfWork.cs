using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NLog;

namespace FinHorizon.MySync.Infrastructure.Persistence;

internal sealed class UnitOfWork(MySyncDbContext db) : IUnitOfWork
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } unique)
        {
            Logger.Info("Unique index {Constraint} refused a concurrent write", unique.ConstraintName);
            db.ChangeTracker.Clear();
            throw new DuplicateNameException($"Unique index {unique.ConstraintName} violated.", exception);
        }
    }
}
