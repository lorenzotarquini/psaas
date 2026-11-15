using FinHorizon.MySync.Application.Features.Dashboard;
using FinHorizon.MySync.Application.Features.NetWorth;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using FinHorizon.MySync.Domain.NetWorth;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC12-vista-leggera-e-importi-nascosti.md (S4, S5 are frontend scenarios).</summary>
public sealed class UC12_VistaLeggera(PostgresFixture database) : AcceptanceTest(database)
{
    private async Task GivenHeavyAndLightAsync()
    {
        await RestartWithAsync(new Dictionary<string, string> { ["MySync:MaxAccounts"] = "5", ["MySync:MaxDebts"] = "5" });
        TodayIs(new DateOnly(2026, 11, 1));
        var credem = await GivenAccountAsync("Credem", 6000m);
        var gold = await GivenAccountAsync("Oro", 20_000m, "Valuables");
        await GivenAccountAsync("PAC", 3_000m, "SavingsPlan");
        await GivenDebtAsync("Mutuo Prima Casa", credem, 80_000m, "Mortgage");
        await GivenDebtAsync("Auto", credem, 170m);
        TodayIs(new DateOnly(2026, 11, 15));
        // Money moved into valuables leaves the light view: it is no longer "day-to-day" money.
        await Mario.PostAsync("/api/transactions", System.Net.Http.Json.JsonContent.Create(new
        {
            kind = "Transfer",
            date = new DateOnly(2026, 11, 10),
            amount = 1000m,
            accountId = credem.Id,
            counterAccountId = gold.Id
        }));
    }

    [Fact]
    public async Task S1_DashboardLeggera()
    {
        await GivenHeavyAndLightAsync();

        var light = await GetOkAsync<DashboardView>("/api/dashboard?includeStable=false&includeInvestments=false");

        Assert.Equal((5000m, 170m), (light.NetWorth, light.DebtsResidual));
        Assert.Equal(["Credem"], light.Accounts.Where(a => a.Group == PortfolioGroup.Everyday).Select(a => a.Name));
        Assert.Equal(["Auto"], light.Debts.Where(d => d.Group == PortfolioGroup.Everyday).Select(d => d.Name));
    }

    [Fact]
    public async Task S2_DashboardCompleta()
    {
        await GivenHeavyAndLightAsync();

        var full = await GetOkAsync<DashboardView>("/api/dashboard?includeStable=true&includeInvestments=true");
        var byDefault = await DashboardAsync();

        Assert.Equal((29_000m, 80_170m), (full.NetWorth, full.DebtsResidual));
        Assert.Equal(full.NetWorth, byDefault.NetWorth);
    }

    [Fact]
    public async Task S3_AndamentoLeggero()
    {
        await GivenHeavyAndLightAsync();

        var light = await GetOkAsync<NetWorthTrendView>("/api/net-worth?period=30d&includeStable=false&includeInvestments=false");
        var full = await NetWorthAsync("30d");

        Assert.Equal((6000m, 170m), (light.Points[0].NetWorth, light.Points[0].Debts));
        Assert.Equal((5000m, 170m), (light.Points[^1].NetWorth, light.Points[^1].Debts));
        Assert.Equal((-1000m, -16.7m), (light.Change, light.ChangePercent));
        Assert.All(full.Points, point => Assert.Equal(29_000m, point.NetWorth));
    }

    [Fact]
    public async Task S8_SoloInvestimentiEDepositi()
    {
        await GivenHeavyAndLightAsync();

        var investments = await GetOkAsync<DashboardView>("/api/dashboard?includeStable=false&includeInvestments=true");

        Assert.Equal((8000m, 170m), (investments.NetWorth, investments.DebtsResidual));
        Assert.Equal(["PAC"], investments.Accounts.Where(a => a.Group == PortfolioGroup.Investments).Select(a => a.Name));
    }
}
