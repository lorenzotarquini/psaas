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
        builder.HasOne<Account>().WithMany().HasForeignKey(transaction => transaction.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(transaction => transaction.CounterAccountId).OnDelete(DeleteBehavior.Restrict);
        // The effects on the balances, replaced as a whole when the movement is amended.
        builder.OwnsMany(transaction => transaction.Postings, posting =>
        {
            posting.ToTable("Postings");
            posting.WithOwner().HasForeignKey("TransactionId");
            posting.Property<int>("Id");
            posting.HasKey("TransactionId", "Id");
            posting.HasOne<Account>().WithMany().HasForeignKey(p => p.AccountId).OnDelete(DeleteBehavior.Restrict);
            posting.HasIndex(p => p.AccountId);
        });
        builder.Navigation(transaction => transaction.Postings).HasField("_postings").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<Category>().WithMany().HasForeignKey(transaction => transaction.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Debt>().WithMany().HasForeignKey(transaction => transaction.DebtId).OnDelete(DeleteBehavior.Restrict);
        // The order of every list (UC4, UC8) and the per-day sums of the trend (UC7).
        builder.HasIndex(transaction => new { transaction.OwnerId, transaction.Date, transaction.RecordedAt });
    }
}
