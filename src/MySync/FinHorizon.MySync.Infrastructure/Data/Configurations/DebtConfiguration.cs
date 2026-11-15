using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Debts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinHorizon.MySync.Infrastructure.Data.Configurations;

internal sealed class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    public void Configure(EntityTypeBuilder<Debt> builder)
    {
        builder.ToTable("Debts");
        builder.HasKey(debt => debt.Id);
        builder.Property(debt => debt.Id).ValueGeneratedNever();
        builder.Property(debt => debt.OwnerId).IsRequired();
        builder.Property(debt => debt.Kind).HasConversion<string>().HasMaxLength(32);
        builder.MapDescriptor(debt => debt.Descriptor);
        builder.Property(debt => debt.NormalizedName).HasMaxLength(Descriptor.MaxNameLength).IsRequired();
        builder.HasOne<Account>().WithMany().HasForeignKey(debt => debt.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(debt => new { debt.OwnerId, debt.NormalizedName }).IsUnique();
    }
}
