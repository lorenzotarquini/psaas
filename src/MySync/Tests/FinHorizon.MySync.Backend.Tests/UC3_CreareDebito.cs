using System.Net;
using System.Net.Http.Json;
using FinHorizon.MySync.Application.Features.Debts;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC3-creare-debito.md</summary>
public sealed class UC3_CreareDebito(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact]
    public async Task S1_DebitoCreato()
    {
        var account = await GivenAccountAsync("Conto Intesa", 2500m);

        var response = await Mario.PostAsJsonAsync("/api/debts", new
        {
            kind = "Mortgage",
            name = "Mutuo casa",
            description = "Tasso fisso 25 anni",
            tag = "Casa",
            accountId = account.Id,
            residual = 120_000m
        });

        var debt = await ReadAsync<DebtView>(response, HttpStatusCode.Created);
        Assert.Equal(120_000m, debt.Residual);
        Assert.Equal(account.Id, debt.AccountId);
        var dashboard = await DashboardAsync();
        Assert.Equal((2500m, 120_000m), (dashboard.NetWorth, dashboard.DebtsResidual));
    }

    [Fact]
    public async Task S2_DatiObbligatoriMancanti()
    {
        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/debts", new { }));

        Assert.Equal(
            new Dictionary<string, string> { ["kind"] = "Required", ["name"] = "Required", ["accountId"] = "Required", ["residual"] = "Required" },
            errors);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-100.00")]
    public async Task S3_ResiduoNonPositivo(string residual)
    {
        var account = await GivenAccountAsync("Conto Intesa", 0m);

        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/debts", DebtBody("Auto", account.Id, decimal.Parse(residual, System.Globalization.CultureInfo.InvariantCulture))));

        Assert.Equal("InvalidValue", errors["residual"]);
    }

    [Fact]
    public async Task S4_ContoDiUnAltroUtente()
    {
        var annasAccount = await GivenAccountAsync("Conto Anna", 0m, user: UserClient("Anna"));

        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/debts", DebtBody("Auto", annasAccount.Id, 100m)));

        Assert.Equal("InvalidValue", errors["accountId"]);
    }

    [Fact]
    public async Task S5_NomeGiaUsato()
    {
        var account = await GivenAccountAsync("Conto Intesa", 0m);
        await GivenDebtAsync("Mutuo casa", account, 100m);

        // The default limit is one debt: a second one would hit the limit first, so raise it here.
        await RestartWithAsync(new Dictionary<string, string> { ["MySync:MaxDebts"] = "5" });
        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/debts", DebtBody("MUTUO CASA", account.Id, 100m)));

        Assert.Equal("NotUnique", errors["name"]);
    }

    [Fact]
    public async Task S6_LimiteRaggiunto()
    {
        var account = await GivenAccountAsync("Conto Intesa", 0m);
        await GivenDebtAsync("Mutuo casa", account, 100m);

        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/debts", DebtBody("Auto", account.Id, 100m)));

        Assert.Equal("LimitReached", errors["limit"]);
    }

    [Fact]
    public async Task S7_ZeroDebitiLiDisattiva()
    {
        await RestartWithAsync(new Dictionary<string, string> { ["MySync:MaxDebts"] = "0" });
        var account = await GivenAccountAsync("Conto Intesa", 0m);

        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/debts", DebtBody("Auto", account.Id, 100m)));

        Assert.Equal("LimitReached", errors["limit"]);
        Assert.Equal(0, (await GetOkAsync<DebtList>("/api/debts")).Max);
    }

    [Fact, Trait("Scenario", "UC3-S8")]
    public async Task S8_DebitoConRataMensile()
    {
        var account = await GivenAccountAsync("Conto Intesa", 2500m);

        var debt = await ReadAsync<DebtView>(
            await Mario.PostAsJsonAsync("/api/debts", new { kind = "Loan", name = "Auto", accountId = account.Id, residual = 4000m, monthlyPayment = 250m }),
            HttpStatusCode.Created);

        Assert.Equal((250m, 4000m), (debt.MonthlyPayment, debt.Residual));
        Assert.Equal((250m, 4000m), ((await DebtAsync(debt.Id)).MonthlyPayment, (await DebtAsync(debt.Id)).Residual));
    }

    [Theory, Trait("Scenario", "UC3-S9")]
    [InlineData(0, "InvalidValue")]
    [InlineData(-50, "InvalidValue")]
    [InlineData(10.001, "InvalidFormat")]
    public async Task S9_RataNonValida(decimal payment, string code)
    {
        var account = await GivenAccountAsync("Conto Intesa", 2500m);

        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/debts", new { kind = "Loan", name = "Auto", accountId = account.Id, residual = 4000m, monthlyPayment = payment }));

        Assert.Equal(code, errors["monthlyPayment"]);
    }
}
