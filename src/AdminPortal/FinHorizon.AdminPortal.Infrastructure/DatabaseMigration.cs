using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using FinHorizon.AdminPortal.Infrastructure.Data.DbContexts;

namespace FinHorizon.AdminPortal.Infrastructure;

public static class DatabaseMigration
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>Brings adminportal-db to the latest migration at every start: pending migrations are applied, none is a no-op.</summary>
    public static async Task MigrateAdminPortalDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminPortalDbContext>();
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).ToArray();
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        Logger.Info("Database adminportal-db migrated: {PendingMigrations} applied", pending);
    }
}
