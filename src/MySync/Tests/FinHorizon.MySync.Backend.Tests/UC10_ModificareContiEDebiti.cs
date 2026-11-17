using System.Net;
using System.Net.Http.Json;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Debts;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using FinHorizon.MySync.Domain.Accounts;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC10-modificare-conti-e-debiti.md (S6, S7 are frontend scenarios).</summary>
public sealed class UC10_ModificareContiEDebiti(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact]
    public async Task S1_ModificaContoRicalcolaIlPatrimonio()
    {
        TodayIs(new DateOnly(2026, 11, 10));
        var credem = await GivenAccountAsync("Credem", 6000m);
        TodayIs(new DateOnly(2026, 11, 15));
        await GivenExpenseAsync(100m, credem, await GivenCategoryAsync("Spesa"), date: new DateOnly(2026, 11, 12));

        var response = await Mario.PutAsJsonAsync($"/api/accounts/{credem.Id}", new
        {
            kind = "VirtualBank",
            name = "Credem online",
            tag = "BANCA",
            initialBalance = 7600m
        });

        var account = await ReadAsync<AccountView>(response, HttpStatusCode.OK);
        Assert.Equal((AccountKind.VirtualBank, "Credem online", 7500m), (account.Kind, account.Name, account.Balance));
        Assert.Equal(new DateOnly(2026, 11, 10), account.OpenedOn);
        Assert.Equal(7500m, (await DashboardAsync()).NetWorth);
        // The whole trend follows the new initial balance.
        Assert.Equal(7600m, (await NetWorthAsync("30d")).Points[0].NetWorth);
    }

    [Fact]
    public async Task S2_NomeGiaUsatoDaUnAltroConto()
    {
        var credem = await GivenAccountAsync("Credem", 0m);
        await GivenAccountAsync("Revolut", 0m, "VirtualBank");

        var errors = await ErrorsAsync(await Mario.PutAsJsonAsync($"/api/accounts/{credem.Id}", AccountBody("revolut", 0m)));

        Assert.Equal("NotUnique", errors["name"]);
    }

    [Fact]
    public async Task S3_ModificaDebito()
    {
        var credem = await GivenAccountAsync("Credem", 6000m);
        var revolut = await GivenAccountAsync("Revolut", 23m, "VirtualBank");
        var mortgage = await GivenDebtAsync("Mutuo Prima Casa", credem, 170m, "Mortgage");

        var response = await Mario.PutAsJsonAsync($"/api/debts/{mortgage.Id}", DebtBody("Mutuo casa", revolut.Id, 200m, "Mortgage"));

        var debt = await ReadAsync<DebtView>(response, HttpStatusCode.OK);
        Assert.Equal(("Mutuo casa", revolut.Id, 200m), (debt.Name, debt.AccountId, debt.Residual));
        Assert.Equal(200m, (await DashboardAsync()).DebtsResidual);
    }

    [Fact]
    public async Task S4_ResiduoSottoIlGiaPagato()
    {
        var credem = await GivenAccountAsync("Credem", 6000m);
        var car = await GivenDebtAsync("Auto", credem, 300m);
        await GivenExpenseAsync(100m, credem, await GivenCategoryAsync("Rate"), car);

        var errors = await ErrorsAsync(await Mario.PutAsJsonAsync($"/api/debts/{car.Id}", DebtBody("Auto", credem.Id, 99.99m)));

        Assert.Equal("InvalidValue", errors["residual"]);
        Assert.Equal(200m, (await DebtAsync(car.Id)).Residual);
    }

    [Theory]
    [InlineData("accounts")]
    [InlineData("debts")]
    public async Task S5_RecordDiUnAltroUtente(string resource)
    {
        var credem = await GivenAccountAsync("Credem", 0m);
        var debt = await GivenDebtAsync("Auto", credem, 100m);
        var id = resource == "accounts" ? credem.Id : debt.Id;
        object body = resource == "accounts" ? AccountBody("Mio", 1m) : DebtBody("Mio", credem.Id, 1m);

        var response = await UserClient("Anna").PutAsJsonAsync($"/api/{resource}/{id}", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(("Credem", "Auto"), ((await AccountAsync(credem.Id)).Name, (await DebtAsync(debt.Id)).Name));
    }

    [Fact, Trait("Scenario", "UC10-S8")]
    public async Task S8_ModificareLaRataMensile()
    {
        var credem = await GivenAccountAsync("Credem", 6000m);
        var car = await ReadAsync<DebtView>(
            await Mario.PostAsJsonAsync("/api/debts", new { kind = "Loan", name = "Auto", accountId = credem.Id, residual = 4000m, monthlyPayment = 250m }),
            HttpStatusCode.Created);

        var changed = await ReadAsync<DebtView>(
            await Mario.PutAsJsonAsync($"/api/debts/{car.Id}", new { kind = "Loan", name = "Auto", accountId = credem.Id, residual = 4000m, monthlyPayment = 300m }),
            HttpStatusCode.OK);
        Assert.Equal((300m, 4000m), (changed.MonthlyPayment, changed.Residual));

        var removed = await ReadAsync<DebtView>(
            await Mario.PutAsJsonAsync($"/api/debts/{car.Id}", DebtBody("Auto", credem.Id, 4000m)),
            HttpStatusCode.OK);
        Assert.Null(removed.MonthlyPayment);
    }
}
