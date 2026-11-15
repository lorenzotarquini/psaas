using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Debts;
using Xunit;
using static FinHorizon.MySync.Domain.Tests.Builders;

namespace FinHorizon.MySync.Domain.Tests;

/// <summary>UC10: what can change on accounts and debts, and what cannot.</summary>
public sealed class UpdateTests
{
    [Fact]
    public void Account_ChangesEverythingButTheOpeningDay()
    {
        var account = Account(initialBalance: 1000m, openedOn: Today.AddDays(-10));

        account.Update(AccountKind.VirtualBank, new Descriptor("Revolut", "Carta", "Viaggi"), Euro(1500m));

        Assert.Equal((AccountKind.VirtualBank, "Revolut", "REVOLUT", 1500m), (account.Kind, account.Descriptor.Name, account.NormalizedName, account.InitialBalance.Amount));
        Assert.Equal(Today.AddDays(-10), account.OpenedOn);
    }

    [Fact]
    public void Debt_ResidualCannotGoBelowWhatWasPaid()
    {
        var debt = Debt(Account(), residual: 500m);

        Assert.Throws<ArgumentException>(() => debt.Update(DebtKind.Loan, new Descriptor("Auto", null, null), debt.AccountId, Euro(99m), Euro(100m)));
        debt.Update(DebtKind.Mortgage, new Descriptor("Mutuo", null, null), debt.AccountId, Euro(100m), Euro(100m));

        Assert.Equal((DebtKind.Mortgage, "MUTUO", Euro(0m)), (debt.Kind, debt.NormalizedName, debt.ResidualAfter(Euro(100m))));
    }
}
