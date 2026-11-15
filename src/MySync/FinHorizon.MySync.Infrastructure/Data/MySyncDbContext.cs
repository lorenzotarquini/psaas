using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace FinHorizon.MySync.Infrastructure.Data;

/// <summary>The MySync database (mysync-db). Mappings live in <see cref="Configurations"/>, one class per entity.</summary>
public sealed class MySyncDbContext(DbContextOptions<MySyncDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Debt> Debts => Set<Debt>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MySyncDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        configurationBuilder.Properties<Domain.Common.Money>().HaveConversion<Configurations.MoneyConverter>().HavePrecision(18, 2);
        configurationBuilder.Properties<Domain.Common.OwnerId>().HaveConversion<Configurations.OwnerIdConverter>().HaveMaxLength(64);
    }
}
