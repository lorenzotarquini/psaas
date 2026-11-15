using FinHorizon.MySync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests.Infrastruttura;

/// <summary>One real Postgres (Docker) for the whole integration suite, migrated like production.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = new MySyncDbContext(new DbContextOptionsBuilder<MySyncDbContext>().UseNpgsql(ConnectionString).Options);
        await db.Database.MigrateAsync();
    }

    /// <summary>Every scenario starts from an empty database; new tables are emptied without touching this fixture.</summary>
    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            DO $$ DECLARE tables text;
            BEGIN
              SELECT string_agg(format('%I', tablename), ', ') INTO tables
              FROM pg_tables WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory';
              EXECUTE 'TRUNCATE ' || tables || ' CASCADE';
            END $$;
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

// The scenarios share one database, so they run one at a time.
[CollectionDefinition(Name)]
public sealed class BackendScenarios : ICollectionFixture<PostgresFixture>
{
    public const string Name = "MySync Backend";
}
