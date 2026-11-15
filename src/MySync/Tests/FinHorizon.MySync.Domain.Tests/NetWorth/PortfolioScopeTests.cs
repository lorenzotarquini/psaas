using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.NetWorth;
using Xunit;

namespace FinHorizon.MySync.Domain.Tests.NetWorth;

/// <summary>UC12: every kind of account and debt belongs to one group; each switch adds a group to the view.</summary>
public sealed class PortfolioScopeTests
{
    [Theory]
    [InlineData(AccountKind.CurrentAccount, PortfolioGroup.Everyday)]
    [InlineData(AccountKind.VirtualBank, PortfolioGroup.Everyday)]
    [InlineData(AccountKind.Cash, PortfolioGroup.Everyday)]
    [InlineData(AccountKind.Other, PortfolioGroup.Everyday)]
    [InlineData(AccountKind.Valuables, PortfolioGroup.Stable)]
    [InlineData(AccountKind.DepositAccount, PortfolioGroup.Investments)]
    [InlineData(AccountKind.Investments, PortfolioGroup.Investments)]
    [InlineData(AccountKind.SavingsPlan, PortfolioGroup.Investments)]
    public void Accounts_HaveTheirGroup(AccountKind kind, PortfolioGroup group) => Assert.Equal(group, PortfolioScope.GroupOf(kind));

    [Theory]
    [InlineData(DebtKind.Mortgage, PortfolioGroup.Stable)]
    [InlineData(DebtKind.Loan, PortfolioGroup.Everyday)]
    [InlineData(DebtKind.Overdraft, PortfolioGroup.Everyday)]
    [InlineData(DebtKind.Other, PortfolioGroup.Everyday)]
    public void Debts_HaveTheirGroup(DebtKind kind, PortfolioGroup group) => Assert.Equal(group, PortfolioScope.GroupOf(kind));

    [Theory]
    [InlineData(false, false, new[] { AccountKind.CurrentAccount })]
    [InlineData(true, false, new[] { AccountKind.CurrentAccount, AccountKind.Valuables })]
    [InlineData(false, true, new[] { AccountKind.CurrentAccount, AccountKind.DepositAccount, AccountKind.Investments, AccountKind.SavingsPlan })]
    public void Switches_AddTheirGroup(bool stable, bool investments, AccountKind[] included)
    {
        var scope = PortfolioScope.For(stable, investments);
        var sample = new[] { AccountKind.CurrentAccount, AccountKind.Valuables, AccountKind.DepositAccount, AccountKind.Investments, AccountKind.SavingsPlan };

        Assert.Equal(included, sample.Where(scope.Includes));
        Assert.Equal(sample.Except(included).Order(), scope.ExcludedAccountKinds.Intersect(sample).Order());
    }

    [Fact]
    public void Mortgages_FollowTheStableSwitch()
    {
        Assert.False(PortfolioScope.For(includeStable: false, includeInvestments: true).Includes(DebtKind.Mortgage));
        Assert.True(PortfolioScope.For(includeStable: true, includeInvestments: false).Includes(DebtKind.Mortgage));
        Assert.True(PortfolioScope.For(includeStable: false, includeInvestments: false).Includes(DebtKind.Loan));
    }

    [Fact]
    public void Everything_ExcludesNothing()
    {
        Assert.Empty(PortfolioScope.Everything.ExcludedAccountKinds);
        Assert.Empty(PortfolioScope.Everything.ExcludedDebtKinds);
    }
}
