using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC4-consultare-dashboard.md</summary>
public sealed class UC4_ConsultareDashboard(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact]
    public async Task S1_Riepilogo()
    {
        var intesa = await GivenAccountAsync("Conto Intesa", 2500m);
        var contanti = await GivenAccountAsync("Contanti", 200m, "Cash");
        await GivenDebtAsync("Mutuo casa", intesa, 120_000m, "Mortgage");
        await GivenIncomeAsync(1800m, intesa, await GivenCategoryAsync("Stipendio", "Income"));
        await GivenExpenseAsync(150m, intesa, await GivenCategoryAsync("Bollette"));

        var dashboard = await DashboardAsync();

        Assert.Equal(4150m, dashboard.Accounts.Single(a => a.Id == intesa.Id).Balance);
        Assert.Equal(200m, dashboard.Accounts.Single(a => a.Id == contanti.Id).Balance);
        Assert.Equal((4350m, 120_000m), (dashboard.NetWorth, dashboard.DebtsResidual));
    }

    [Fact]
    public async Task S2_UltimiMovimenti()
    {
        TodayIs(new DateOnly(2026, 11, 1));
        var account = await GivenAccountAsync("Conto Intesa", 0m);
        var category = await GivenCategoryAsync("Spesa");
        TodayIs(new DateOnly(2026, 11, 15));
        for (var day = 1; day <= 12; day++)
        {
            await GivenExpenseAsync(day, account, category, date: new DateOnly(2026, 11, day));
        }

        var recent = (await DashboardAsync()).RecentTransactions;

        Assert.Equal(10, recent.Count);
        Assert.Equal(new DateOnly(2026, 11, 12), recent[0].Date);
        Assert.Equal(recent.OrderByDescending(t => t.Date), recent);
    }

    [Fact]
    public async Task S3_RataRiduceIlResiduo()
    {
        var account = await GivenAccountAsync("Conto Intesa", 1000m);
        var mortgage = await GivenDebtAsync("Mutuo casa", account, 120_000m, "Mortgage");
        await GivenExpenseAsync(650m, account, await GivenCategoryAsync("Casa"), mortgage);

        Assert.Equal(119_350m, (await DebtAsync(mortgage.Id)).Residual);
    }

    [Fact]
    public async Task S4_SoloIPropriDati()
    {
        await GivenAccountAsync("Conto Mario", 5000m);
        var anna = UserClient("Anna");
        var annas = await GivenAccountAsync("Conto Anna", 300m, user: anna);

        var dashboard = await DashboardAsync(anna);

        Assert.Equal([annas.Id], dashboard.Accounts.Select(a => a.Id));
        Assert.Empty(dashboard.Debts);
        Assert.Empty(dashboard.RecentTransactions);
        Assert.Equal(300m, dashboard.NetWorth);
    }

    [Fact]
    public async Task S5_DebitiEstintiInFondo()
    {
        await RestartWithAsync(new Dictionary<string, string> { ["MySync:MaxDebts"] = "3" });
        var account = await GivenAccountAsync("Conto Intesa", 10_000m);
        var category = await GivenCategoryAsync("Rate");
        var small = await GivenDebtAsync("Piccolo", account, 100m);
        var paidOff = await GivenDebtAsync("Estinto", account, 50m);
        var big = await GivenDebtAsync("Grande", account, 5000m);
        await GivenExpenseAsync(50m, account, category, paidOff);

        var debts = (await DashboardAsync()).Debts;

        Assert.Equal([big.Id, small.Id, paidOff.Id], debts.Select(d => d.Id));
        Assert.True(debts[^1].PaidOff);
    }
}
