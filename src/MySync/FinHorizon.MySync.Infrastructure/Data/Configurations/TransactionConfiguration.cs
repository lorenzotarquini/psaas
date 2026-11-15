using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinHorizon.MySync.Infrastructure.Data.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Id).ValueGeneratedNever();
        builder.Property(transaction => transaction.OwnerId).IsRequired();
        builder.Property(transaction => transaction.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(transaction => transaction.Note).HasMaxLength(TransactionData.MaxNoteLength);
        builder.Ignore(transaction => transaction.SignedAmount);
        builder.HasOne<Account>().WithMany().HasForeignKey(transaction => transaction.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(transaction => transaction.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Debt>().WithMany().HasForeignKey(transaction => transaction.DebtId).OnDelete(DeleteBehavior.Restrict);
        // The order of every list (UC4, UC8) and the per-day sums of the trend (UC7).
        builder.HasIndex(transaction => new { transaction.OwnerId, transaction.Date, transaction.RecordedAt });
    }
}
