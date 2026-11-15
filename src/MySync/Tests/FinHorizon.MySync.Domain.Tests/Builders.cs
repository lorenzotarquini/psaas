using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.Transactions;

namespace FinHorizon.MySync.Domain.Tests;

/// <summary>Readable test data: "Mario" and his records.</summary>
internal static class Builders
{
    public static readonly OwnerId Mario = new("kc-mario");
    public static readonly DateOnly Today = new(2026, 11, 15);

    public static Money Euro(decimal amount) => new(amount);

    public static Account Account(decimal initialBalance = 1000m, DateOnly? openedOn = null, string name = "Conto Intesa") =>
        Domain.Accounts.Account.Open(Mario, AccountKind.CurrentAccount, new Descriptor(name, null, null), "EUR", Euro(initialBalance), openedOn ?? Today.AddDays(-30));

    public static Debt Debt(Account account, decimal residual = 500m, DateOnly? createdOn = null) =>
        Domain.Debts.Debt.Take(Mario, DebtKind.Loan, new Descriptor("Auto", null, null), account.Id, "EUR", Euro(residual), createdOn ?? account.OpenedOn);

    public static Category Category(TransactionKind kind = TransactionKind.Expense, string name = "Spesa") =>
        Domain.Categories.Category.Create(Mario, kind, name);

    public static TransactionData Expense(Account account, Category category, decimal amount, DateOnly? date = null, Guid? debtId = null) =>
        new(TransactionKind.Expense, date ?? Today, Euro(amount), account.Id, category.Id, debtId, null);
}
