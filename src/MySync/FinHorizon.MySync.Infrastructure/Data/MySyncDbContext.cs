using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.Preferences;
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

    public DbSet<UserPreferences> Preferences => Set<UserPreferences>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MySyncDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        // 8 decimals for cryptocurrencies (UC13-R2); 20 digits before the point are plenty for any currency.
        configurationBuilder.Properties<Domain.Common.Money>().HaveConversion<Configurations.MoneyConverter>().HavePrecision(28, Domain.Currencies.CurrencyRules.MaxDecimals);
        configurationBuilder.Properties<Domain.Common.OwnerId>().HaveConversion<Configurations.OwnerIdConverter>().HaveMaxLength(64);
    }
}
