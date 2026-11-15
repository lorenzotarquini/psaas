using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;
using FinHorizon.MySync.Domain.Transactions.Rules;
using Xunit;
using static FinHorizon.MySync.Domain.Tests.Builders;

namespace FinHorizon.MySync.Domain.Tests.Transactions;

/// <summary>The rules behind UC5 and UC8, one at a time.</summary>
public sealed class TransactionPolicyTests
{
    /// <summary>Every rule of the domain, discovered as the application does.</summary>
    private static readonly TransactionPolicy Policy = new(
        typeof(ITransactionRule).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(ITransactionRule).IsAssignableFrom(type))
            .Select(type => (ITransactionRule)Activator.CreateInstance(type)!));

    [Fact]
    public void ValidExpense_HasNoErrors()
    {
        var account = Account();
        var category = Category();

        Assert.Empty(Policy.Check(Context(Expense(account, category, 85.40m), account, category)));
    }

    [Fact]
    public void MissingAccount_IsInvalidValueOnAccount()
    {
        var category = Category();
        var data = Expense(Account(), category, 10m);

        Assert.Contains(new FieldError("accountId", ErrorCodes.InvalidValue), Policy.Check(Context(data, null, category)));
    }

    [Fact]
    public void IncomeCategoryOnAnExpense_IsInvalidValueOnCategory()
    {
        var account = Account();
        var category = Category(TransactionKind.Income, "Stipendio");

        Assert.Equal([new FieldError("categoryId", ErrorCodes.InvalidValue)], Policy.Check(Context(Expense(account, category, 10m), account, category)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-31)]
    public void DateOutsideAccountLifetime_IsInvalidValueOnDate(int daysFromToday)
    {
        var account = Account(openedOn: Today.AddDays(-30));
        var category = Category();

        Assert.Equal(
            [new FieldError("date", ErrorCodes.InvalidValue)],
            Policy.Check(Context(Expense(account, category, 10m, Today.AddDays(daysFromToday)), account, category)));
    }

    [Fact]
    public void OpeningDayAndToday_AreBothAllowed()
    {
        var account = Account(openedOn: Today.AddDays(-30));
        var category = Category();

        Assert.Empty(Policy.Check(Context(Expense(account, category, 10m, account.OpenedOn), account, category)));
        Assert.Empty(Policy.Check(Context(Expense(account, category, 10m, Today), account, category)));
    }

    [Theory]
    [InlineData("300.00", true)]
    [InlineData("300.01", false)]
    public void Installment_CannotExceedTheResidual(string amount, bool valid)
    {
        var account = Account();
        var category = Category();
        var debt = Debt(account, residual: 300m);
        var data = Expense(account, category, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), debtId: debt.Id);

        var errors = Policy.Check(Context(data, account, category, debt, residual: 300m));

        Assert.Equal(valid, errors.Count == 0);
        if (!valid)
        {
            Assert.Equal([new FieldError("amount", ErrorCodes.InvalidValue)], errors);
        }
    }

    [Fact]
    public void PaidOffDebt_IsInvalidValueOnDebt()
    {
        var account = Account();
        var category = Category();
        var debt = Debt(account);

        var errors = Policy.Check(Context(Expense(account, category, 1m, debtId: debt.Id), account, category, debt, residual: 0m));

        Assert.Equal([new FieldError("debtId", ErrorCodes.InvalidValue)], errors);
    }

    [Fact]
    public void IncomeLinkedToADebt_IsInvalidValueOnDebt()
    {
        var account = Account();
        var category = Category(TransactionKind.Income, "Rimborso");
        var debt = Debt(account);
        var data = new TransactionData(TransactionKind.Income, Today, Euro(10m), account.Id, category.Id, debt.Id, null);

        Assert.Equal([new FieldError("debtId", ErrorCodes.InvalidValue)], Policy.Check(Context(data, account, category, debt, residual: 500m)));
    }

    [Fact]
    public void InstallmentBeforeTheDebtExisted_IsInvalidValueOnDate()
    {
        var account = Account(openedOn: Today.AddDays(-30));
        var category = Category();
        var debt = Debt(account, createdOn: Today.AddDays(-5));

        var errors = Policy.Check(Context(Expense(account, category, 10m, Today.AddDays(-6), debt.Id), account, category, debt, residual: 500m));

        Assert.Equal([new FieldError("date", ErrorCodes.InvalidValue)], errors);
    }

    [Fact]
    public void Policy_ReportsOneErrorPerField()
    {
        var category = Category();
        var data = Expense(Account(), category, 10m, Today.AddDays(1));

        var errors = Policy.Check(Context(data, null, null));

        Assert.Equal(errors.Count, errors.Select(error => error.Field).Distinct().Count());
        Assert.Equal(3, errors.Count);
    }

    private static TransactionContext Context(
        TransactionData data,
        Accounts.Account? account,
        Categories.Category? category,
        Debts.Debt? debt = null,
        decimal residual = 0m) =>
        new(data, account, category, debt, Euro(residual), Today);
}
