using FinHorizon.MySync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace FinHorizon.MySync.Infrastructure;

public static class DatabaseMigration
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>Brings mysync-db to the latest migration; used at start when Database:MigrateOnStartup is on.</summary>
    public static async Task MigrateMySyncDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MySyncDbContext>();
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        Logger.Info("Database mysync-db migrated");
    }
}
