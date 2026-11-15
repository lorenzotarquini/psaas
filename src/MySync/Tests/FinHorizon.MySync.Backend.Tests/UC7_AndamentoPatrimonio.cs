using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC7-andamento-patrimonio.md</summary>
public sealed class UC7_AndamentoPatrimonio(PostgresFixture database) : AcceptanceTest(database)
{
    private static readonly DateOnly Nov12 = new(2026, 11, 12);

    [Fact]
    public async Task S1_AndamentoGiornaliero()
    {
        await GivenUc7S1HistoryAsync();

        var trend = await NetWorthAsync("30d");

        Assert.Equal(
            [
                (Nov12, 1000m, 0m),
                (Nov12.AddDays(1), 1000m, 500m),
                (Nov12.AddDays(2), 900m, 400m),
                (Nov12.AddDays(3), 900m, 400m)
            ],
            trend.Points.Select(p => (p.Date, p.NetWorth, p.Debts)));
    }

    [Fact]
    public async Task S2_MovimentoConDataPassata()
    {
        var (account, _) = await GivenUc7S1HistoryAsync();

        await GivenIncomeAsync(200m, account, await GivenCategoryAsync("Regali", "Income"), Nov12.AddDays(1));

        var points = (await NetWorthAsync("30d")).Points;
        Assert.Equal([1000m, 1200m, 1100m, 1100m], points.Select(point => point.NetWorth));
    }

    [Fact]
    public async Task S3_VariazioneSulPeriodo()
    {
        TodayIs(new DateOnly(2026, 10, 1));
        var account = await GivenAccountAsync("Conto Intesa", 10_000m);
        TodayIs(new DateOnly(2026, 11, 15));
        await GivenIncomeAsync(500m, account, await GivenCategoryAsync("Bonus", "Income"));

        var trend = await NetWorthAsync("30d");

        Assert.Equal((500m, 5.0m), (trend.Change, trend.ChangePercent));
    }

    [Fact]
    public async Task S4_PeriodoNonValido()
    {
        var errors = await ErrorsAsync(await Mario.GetAsync("/api/net-worth?period=7y"));

        Assert.Equal("InvalidValue", errors["period"]);
    }

    [Theory]
    [InlineData("90d")]
    [InlineData("12m")]
    [InlineData("all")]
    public async Task S5_PeriodiLunghiFinisconoOggi(string period)
    {
        TodayIs(new DateOnly(2024, 1, 10));
        await GivenAccountAsync("Conto Intesa", 100m);
        TodayIs(new DateOnly(2026, 11, 15));

        var trend = await NetWorthAsync(period);

        Assert.Equal(period, trend.Period);
        Assert.Equal(Today, trend.Points[^1].Date);
        Assert.All(trend.Points, point => Assert.Equal(100m, point.NetWorth));
    }

    /// <summary>UC7-S1: account on the 12th, debt on the 13th, installment on the 14th, today the 15th.</summary>
    private async Task<(Application.Features.Accounts.AccountView Account, Application.Features.Debts.DebtView Debt)> GivenUc7S1HistoryAsync()
    {
        TodayIs(Nov12);
        var account = await GivenAccountAsync("Conto Intesa", 1000m);
        TodayIs(Nov12.AddDays(1));
        var car = await GivenDebtAsync("Auto", account, 500m);
        TodayIs(Nov12.AddDays(2));
        await GivenExpenseAsync(100m, account, await GivenCategoryAsync("Rate"), car);
        TodayIs(Nov12.AddDays(3));
        return (account, car);
    }
}
