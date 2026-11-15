using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.Transactions;
using Xunit;
using static FinHorizon.MySync.Domain.Tests.Builders;

namespace FinHorizon.MySync.Domain.Tests;

public sealed class EntitiesTests
{
    [Fact]
    public void Account_OpensTodayWithItsInitialBalance()
    {
        var account = Domain.Accounts.Account.Open(Mario, AccountKind.CurrentAccount, new Descriptor("Conto BPM", null, null), "EUR", Euro(-300m), Today);

        Assert.Equal(Today, account.OpenedOn);
        Assert.Equal(Euro(-300m), account.InitialBalance);
        Assert.Equal(Euro(-250m), account.BalanceAfter(Euro(50m)));
    }

    [Fact]
    public void Account_RejectsSubCentBalance() =>
        Assert.Throws<ArgumentException>(() => Domain.Accounts.Account.Open(Mario, AccountKind.Cash, new Descriptor("x", null, null), "EUR", Euro(1.001m), Today));

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Debt_NeedsAPositiveResidual(decimal residual) =>
        Assert.Throws<ArgumentException>(() => Domain.Debts.Debt.Take(Mario, DebtKind.Mortgage, new Descriptor("Mutuo", null, null), Guid.NewGuid(), "EUR", Euro(residual), Today));

    [Fact]
    public void Debt_ResidualDropsWithInstallments() =>
        Assert.Equal(Euro(119_350m), Domain.Debts.Debt.Take(Mario, DebtKind.Mortgage, new Descriptor("Mutuo", null, null), Guid.NewGuid(), "EUR", Euro(120_000m), Today).ResidualAfter(Euro(650m)));

    [Fact]
    public void Category_KeepsTheTypedNameAndNormalizesTheKey()
    {
        var category = Category(name: " palestra ");

        Assert.Equal("palestra", category.Name);
        Assert.Equal("PALESTRA", category.NormalizedName);
    }

    [Fact]
    public void Transaction_ExpenseLowersTheBalance()
    {
        var account = Account();
        var transaction = Transaction.Record(Mario, Expense(account, Category(), 85.40m), DateTimeOffset.UnixEpoch);

        Assert.Equal([(account.Id, Euro(-85.40m))], transaction.Postings.Select(p => (p.AccountId, p.Amount)));
    }

    [Fact]
    public void Transaction_AmendKeepsIdentityAndRecordingTime()
    {
        var account = Account();
        var transaction = Transaction.Record(Mario, Expense(account, Category(), 85.40m), DateTimeOffset.UnixEpoch);
        var id = transaction.Id;

        transaction.Amend(Expense(account, Category(), 58.40m) with { Note = "  corretto " });

        Assert.Equal(id, transaction.Id);
        Assert.Equal(DateTimeOffset.UnixEpoch, transaction.RecordedAt);
        Assert.Equal(Euro(58.40m), transaction.Amount);
        Assert.Equal("corretto", transaction.Note);
    }

    [Fact]
    public void Transaction_IncomeCannotBeAnInstallment()
    {
        var account = Account();
        var data = new TransactionData(TransactionKind.Income, Today, Euro(10m), account.Id, Guid.NewGuid(), Guid.NewGuid(), null);

        Assert.Throws<ArgumentException>(() => Transaction.Record(Mario, data, DateTimeOffset.UnixEpoch));
    }
}
