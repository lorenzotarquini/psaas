using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinHorizon.MySync.Infrastructure.Data.Configurations;

/// <summary>UC16: one row per subscription; name and description as plain columns, as for accounts and debts.</summary>
internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Id).ValueGeneratedNever();
        builder.Property(subscription => subscription.OwnerId).IsRequired();
        builder.Property(subscription => subscription.Currency).HasMaxLength(CurrencyConfiguration.CodeLength).IsRequired();
        builder.MapDescriptor(subscription => subscription.Descriptor);
        builder.Property(subscription => subscription.NormalizedName).HasMaxLength(Descriptor.MaxNameLength).IsRequired();
        builder.Ignore(subscription => subscription.IsCancelled);
        builder.HasOne<Account>().WithMany().HasForeignKey(subscription => subscription.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(subscription => subscription.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(subscription => new { subscription.OwnerId, subscription.NormalizedName }).IsUnique();
    }
}
