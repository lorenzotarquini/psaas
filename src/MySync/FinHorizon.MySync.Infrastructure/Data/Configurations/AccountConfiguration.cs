using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinHorizon.MySync.Infrastructure.Data.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).ValueGeneratedNever();
        builder.Property(account => account.OwnerId).IsRequired();
        builder.Property(account => account.Kind).HasConversion<string>().HasMaxLength(32);
        builder.MapDescriptor(account => account.Descriptor);
        builder.Property(account => account.NormalizedName).HasMaxLength(Descriptor.MaxNameLength).IsRequired();
        // UC2-R2: the last guard against two requests with the same name.
        builder.HasIndex(account => new { account.OwnerId, account.NormalizedName }).IsUnique();
    }
}
