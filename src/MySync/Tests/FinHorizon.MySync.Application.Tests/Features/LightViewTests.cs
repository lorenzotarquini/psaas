using FinHorizon.MySync.Application.Features.Dashboard;
using FinHorizon.MySync.Application.Features.NetWorth;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.NetWorth;
using Xunit;

namespace FinHorizon.MySync.Application.Tests.Features;

/// <summary>UC12 at the application level: Credem 6.000 (everyday), gold 20.000 (stable), PAC 3.000 (investments).</summary>
public sealed class LightViewTests : IDisposable
{
    private readonly ApplicationHarness _app = new();

    public LightViewTests()
    {
        _app.Store.Limits = new(5, 5);
        _app.GivenAccount("Credem", 6000m);
        var gold = GivenAccount(AccountKind.Valuables, "Oro", 20_000m);
        GivenAccount(AccountKind.SavingsPlan, "PAC", 3_000m);
        _app.Store.Debts.Add(Debt.Take(_app.Store.Id, DebtKind.Mortgage, new Descriptor("Mutuo", null, null), gold.Id, "EUR", new Money(80_000m), _app.Store.Today.AddDays(-30)));
        _app.Store.Debts.Add(Debt.Take(_app.Store.Id, DebtKind.Loan, new Descriptor("Auto", null, null), gold.Id, "EUR", new Money(170m), _app.Store.Today.AddDays(-30)));
    }

    [Theory]
    [InlineData(false, false, 6000, 170)]
    [InlineData(true, false, 26_000, 80_170)]
    [InlineData(false, true, 9_000, 170)]
    [InlineData(true, true, 29_000, 80_170)]
    public async Task Dashboard_TotalsFollowTheSwitches(bool stable, bool investments, decimal netWorth, decimal debts)
    {
        var dashboard = (await _app.SendAsync<GetDashboardQuery, DashboardView>(new(stable, investments))).Value!;

        Assert.Equal((netWorth, debts), (dashboard.NetWorth, dashboard.DebtsResidual));
        // The lists stay complete: the frontend hides by group, limits keep counting every item.
        Assert.Equal(
            [PortfolioGroup.Everyday, PortfolioGroup.Stable, PortfolioGroup.Investments],
            dashboard.Accounts.Select(a => a.Group).Order());
        Assert.Equal(2, dashboard.Debts.Count);
    }

    [Fact]
    public async Task Trend_FollowsTheSwitches()
    {
        var everyday = (await _app.SendAsync<GetNetWorthTrendQuery, NetWorthTrendView>(new("30d", false, false))).Value!;
        var investments = (await _app.SendAsync<GetNetWorthTrendQuery, NetWorthTrendView>(new("30d", false, true))).Value!;
        var all = (await _app.SendAsync<GetNetWorthTrendQuery, NetWorthTrendView>(new("30d"))).Value!;

        Assert.All(everyday.Points, point => Assert.Equal((6000m, 170m), (point.NetWorth, point.Debts)));
        Assert.All(investments.Points, point => Assert.Equal((9_000m, 170m), (point.NetWorth, point.Debts)));
        Assert.All(all.Points, point => Assert.Equal((29_000m, 80_170m), (point.NetWorth, point.Debts)));
    }

    private Account GivenAccount(AccountKind kind, string name, decimal balance)
    {
        var account = Account.Open(_app.Store.Id, kind, new Descriptor(name, null, null), "EUR", new Money(balance), _app.Store.Today.AddDays(-30));
        _app.Store.Accounts.Add(account);
        return account;
    }

    public void Dispose() => _app.Dispose();
}
