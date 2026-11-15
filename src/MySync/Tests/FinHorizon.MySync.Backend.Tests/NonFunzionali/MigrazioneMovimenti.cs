using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using FinHorizon.MySync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests.NonFunzionali;

/// <summary>
/// Data recorded before postings existed keep their balances: the "Transfers" migration gives every
/// old expense and income its posting.
/// </summary>
[Collection(BackendScenarios.Name)]
public sealed class MigrazioneMovimenti(PostgresFixture database)
{
    [Fact]
    public async Task MovimentiEsistentiMantengonoISaldi()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Database = $"mysync_migration_{Guid.NewGuid():N}" }.ConnectionString;
        await using var db = new MySyncDbContext(new DbContextOptionsBuilder<MySyncDbContext>().UseNpgsql(connectionString).Options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261115062246_InitialLedger");

        var account = Guid.NewGuid();
        var category = Guid.NewGuid();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO "Accounts" ("Id", "OwnerId", "Kind", "NormalizedName", "InitialBalance", "OpenedOn", "Name")
            VALUES ({account}, 'kc-mario', 'CurrentAccount', 'CREDEM', 1000, DATE '2026-11-01', 'Credem');
            INSERT INTO "Categories" ("Id", "OwnerId", "Kind", "Name", "NormalizedName") VALUES ({category}, 'kc-mario', 'Expense', 'Spesa', 'SPESA');
            INSERT INTO "Transactions" ("Id", "OwnerId", "Kind", "Date", "Amount", "AccountId", "CategoryId", "RecordedAt")
            VALUES ({Guid.NewGuid()}, 'kc-mario', 'Expense', DATE '2026-11-02', 150, {account}, {category}, now()),
                   ({Guid.NewGuid()}, 'kc-mario', 'Income', DATE '2026-11-03', 40, {account}, {category}, now());
            """);

        await migrator.MigrateAsync();

        var balance = await db.Database.SqlQuery<decimal>($"""SELECT SUM("Amount") AS "Value" FROM "Postings" WHERE "AccountId" = {account}""").SingleAsync();
        Assert.Equal(-110m, balance);
        await db.Database.EnsureDeletedAsync();
    }
}
