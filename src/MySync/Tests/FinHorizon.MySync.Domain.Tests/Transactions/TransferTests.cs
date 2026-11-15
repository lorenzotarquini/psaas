using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;
using FinHorizon.MySync.Domain.Transactions.Kinds;
using FinHorizon.MySync.Domain.Transactions.Rules;
using Xunit;
using static FinHorizon.MySync.Domain.Tests.Builders;

namespace FinHorizon.MySync.Domain.Tests.Transactions;

/// <summary>UC9: a transfer moves money between two accounts of the user and is neither an expense nor an income.</summary>
public sealed class TransferTests
{
    private static readonly TransactionPolicy Policy = new(
        typeof(ITransactionRule).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(ITransactionRule).IsAssignableFrom(type))
            .Select(type => (ITransactionRule)Activator.CreateInstance(type)!));

    private static TransactionData Transfer(Accounts.Account from, Accounts.Account to, decimal amount, DateOnly? date = null) =>
        new(TransactionKind.Transfer, date ?? Today, Euro(amount), from.Id, null, null, null, to.Id);

    [Fact]
    public void Postings_TakeFromOneAccountAndGiveToTheOther()
    {
        var from = Account(name: "Credem");
        var to = Account(name: "Revolut");

        var transfer = Transaction.Record(Mario, Transfer(from, to, 500m), DateTimeOffset.UnixEpoch);

        Assert.Equal([(from.Id, Euro(-500m)), (to.Id, Euro(500m))], transfer.Postings.Select(p => (p.AccountId, p.Amount)));
        Assert.Equal(Money.Zero, MovementKinds.NetChange(transfer.Postings));
    }

    [Fact]
    public void Amend_ReplacesThePostings()
    {
        var from = Account(name: "Credem");
        var to = Account(name: "Revolut");
        var transfer = Transaction.Record(Mario, Transfer(from, to, 500m), DateTimeOffset.UnixEpoch);

        transfer.Amend(Transfer(to, from, 20m));

        Assert.Equal([(to.Id, Euro(-20m)), (from.Id, Euro(20m))], transfer.Postings.Select(p => (p.AccountId, p.Amount)));
    }

    [Fact]
    public void ValidTransfer_HasNoErrors()
    {
        var from = Account(name: "Credem");
        var to = Account(name: "Revolut");

        Assert.Empty(Policy.Check(new TransactionContext(Transfer(from, to, 10m), from, null, null, Money.Zero, Today, to)));
    }

    [Fact]
    public void SameAccountOrMissingDestination_IsInvalidValueOnDestination()
    {
        var account = Account();
        var other = Account(name: "Revolut");

        var same = Policy.Check(new TransactionContext(Transfer(account, account, 10m), account, null, null, Money.Zero, Today, account));
        var missing = Policy.Check(new TransactionContext(Transfer(account, other, 10m), account, null, null, Money.Zero, Today, null));

        Assert.Equal([new FieldError("counterAccountId", ErrorCodes.InvalidValue)], same);
        Assert.Equal([new FieldError("counterAccountId", ErrorCodes.InvalidValue)], missing);
    }

    [Fact]
    public void BeforeTheDestinationOpened_IsInvalidValueOnDate()
    {
        var from = Account(openedOn: Today.AddDays(-30));
        var to = Account(name: "Revolut", openedOn: Today.AddDays(-5));

        var errors = Policy.Check(new TransactionContext(Transfer(from, to, 10m, Today.AddDays(-10)), from, null, null, Money.Zero, Today, to));

        Assert.Equal([new FieldError("date", ErrorCodes.InvalidValue)], errors);
    }

    [Fact]
    public void CategoryOrDebt_AreRefused()
    {
        var from = Account(name: "Credem");
        var to = Account(name: "Revolut");
        var debt = Debt(from);
        var data = Transfer(from, to, 10m) with { CategoryId = Guid.NewGuid(), DebtId = debt.Id };

        var errors = Policy.Check(new TransactionContext(data, from, null, debt, Euro(500m), Today, to));

        Assert.Equal(["categoryId", "debtId"], errors.Select(error => error.Field));
    }

    [Fact]
    public void Entity_RefusesATransferWithoutDestination() =>
        Assert.Throws<ArgumentException>(() =>
            Transaction.Record(Mario, new TransactionData(TransactionKind.Transfer, Today, Euro(1m), Guid.NewGuid(), null, null, null), DateTimeOffset.UnixEpoch));

    [Fact]
    public void Category_CannotBeOfTheTransferKind() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Categories.Category.Create(Mario, TransactionKind.Transfer, "Giroconto"));
}
