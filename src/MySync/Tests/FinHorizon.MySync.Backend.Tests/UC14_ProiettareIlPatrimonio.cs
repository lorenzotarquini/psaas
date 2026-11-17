using System.Net;
using System.Net.Http.Json;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Categories;
using FinHorizon.MySync.Application.Features.Debts;
using FinHorizon.MySync.Application.Features.Projection;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC14-proiettare-il-patrimonio.md (the scenarios of the backend; the computation is tested in the frontend).</summary>
public sealed class UC14_ProiettareIlPatrimonio(PostgresFixture database) : AcceptanceTest(database)
{
    private static readonly DateOnly ScenarioToday = new(2026, 11, 17);

    private static readonly Dictionary<string, string> RoomForAll = new() { ["MySync:MaxAccounts"] = "5", ["MySync:MaxDebts"] = "2" };

    private Task<ProjectionView> ProjectionAsync() => GetOkAsync<ProjectionView>("/api/projection");

    private static Dictionary<string, object?> ValidBody() => new()
    {
        ["horizonMonths"] = 12,
        ["income"] = 2000m,
        ["fixedExpenses"] = 700m,
        ["variableExpenses"] = 600m,
        ["saving"] = 300m,
        ["annualReturn"] = 2m,
        ["inflation"] = 2m,
        ["inflationOn"] = false,
        ["payments"] = Array.Empty<object>(),
        ["events"] = Array.Empty<object>(),
        ["goal"] = null,
    };

    [Fact, Trait("Scenario", "UC14-S1")]
    public async Task S1_PuntoDiPartenzaEParametriProposti()
    {
        await RestartWithAsync(RoomForAll);
        TodayIs(new DateOnly(2026, 1, 10));
        var intesa = await GivenAccountAsync("Conto Intesa", 3100m);
        var deposito = await GivenAccountAsync("Deposito", 3200m, "DepositAccount");
        await GivenAccountAsync("Oro", 2000m, "Valuables");
        var auto = await GivenDebtAsync("Auto", intesa, 5500m);
        var affitto = await GivenCategoryAsync("Affitto");
        await Mario.PutAsJsonAsync($"/api/categories/{affitto.Id}/fixed", new { @fixed = true });
        var spesa = await GivenCategoryAsync("Spesa alimentare");
        var svago = await GivenCategoryAsync("Svago");
        var rate = await GivenCategoryAsync("Rate");
        var stipendio = await GivenCategoryAsync("Stipendio", "Income");
        TodayIs(ScenarioToday);
        for (var month = 5; month <= 10; month++)
        {
            var day = new DateOnly(2026, month, 10);
            await GivenIncomeAsync(2000m, intesa, stipendio, day);
            await GivenExpenseAsync(700m, intesa, affitto, date: day);
            await GivenExpenseAsync(400m, intesa, spesa, date: day);
            await GivenExpenseAsync(200m, intesa, svago, date: day);
            await GivenExpenseAsync(250m, intesa, rate, auto, day);
            await TransferAsync(300m, intesa, deposito, day);
        }

        await GivenExpenseAsync(1000m, intesa, svago, date: new DateOnly(2026, 11, 5));

        var projection = await ProjectionAsync();

        Assert.Equal(new ProjectionStartView(3000m, 5000m, 2000m), projection.Start);
        Assert.Equal(new ProjectionDebtView(auto.Id, "Auto", 4000m, null), Assert.Single(projection.Debts));
        var suggested = projection.Suggested;
        Assert.Equal((2000m, 700m, 600m, 300m), (suggested.Income, suggested.FixedExpenses, suggested.VariableExpenses, suggested.Saving));
        Assert.Equal(new ProjectionPaymentView(auto.Id, 250m), Assert.Single(suggested.Payments));
        Assert.Equal(6, projection.MonthsOfData);
        Assert.Equal(("EUR", ScenarioToday), (projection.Currency, projection.Today));
    }

    [Fact, Trait("Scenario", "UC14-S9")]
    public async Task S9_SalvareERipartire()
    {
        var intesa = await GivenAccountAsync("Conto Intesa", 3000m);
        var body = ValidBody();
        body["horizonMonths"] = 60;
        body["variableExpenses"] = 450m;
        body["events"] = new[] { new { month = 2, amount = 1500m, note = "Tredicesima" } };
        body["goal"] = 30000m;

        await ReadAsync<SavedProjectionView>(await Mario.PutAsJsonAsync("/api/projection", body), HttpStatusCode.OK);

        var saved = (await ProjectionAsync()).Saved!;
        Assert.Equal((60, 450m, 30000m), (saved.HorizonMonths, saved.VariableExpenses, saved.Goal));
        Assert.Equal(new ProjectionEventView(2, 1500m, "Tredicesima"), Assert.Single(saved.Events));

        Assert.Equal(HttpStatusCode.NoContent, (await Mario.DeleteAsync("/api/projection")).StatusCode);

        var reset = await ProjectionAsync();
        Assert.Null(reset.Saved);
        Assert.Equal(0m, reset.Suggested.VariableExpenses);
        Assert.Equal(3000m, reset.Start.Liquidity);
        Assert.NotEqual(Guid.Empty, intesa.Id);
    }

    public static TheoryData<string, object?, string, string> InvalidValues => new()
    {
        { "horizonMonths", 0, "horizonMonths", "InvalidValue" },
        { "horizonMonths", 361, "horizonMonths", "InvalidValue" },
        { "fixedExpenses", -1m, "fixedExpenses", "InvalidValue" },
        { "income", 10.001m, "income", "InvalidFormat" },
        { "annualReturn", 25m, "annualReturn", "InvalidValue" },
        { "inflation", -1m, "inflation", "InvalidValue" },
        { "events", Enumerable.Range(1, 11).Select(month => new { month, amount = 10m, note = "x" }).ToArray(), "events", "InvalidValue" },
        { "events", new[] { new { month = 13, amount = 10m, note = "x" } }, "events[0].month", "InvalidValue" },
        { "events", new[] { new { month = 1, amount = 0m, note = "x" } }, "events[0].amount", "InvalidValue" },
        { "events", new[] { new { month = 1, amount = 10m, note = "" } }, "events[0].note", "Required" },
        { "goal", 0m, "goal", "InvalidValue" },
    };

    [Theory, Trait("Scenario", "UC14-S10")]
    [MemberData(nameof(InvalidValues))]
    public async Task S10_DatiNonValidi(string key, object? value, string field, string code)
    {
        await GivenAccountAsync("Conto Intesa", 3000m);
        await Mario.PutAsJsonAsync("/api/projection", ValidBody());
        var body = ValidBody();
        body[key] = value;

        var errors = await ErrorsAsync(await Mario.PutAsJsonAsync("/api/projection", body));

        Assert.Equal(code, errors[field]);
        Assert.Equal(12, (await ProjectionAsync()).Saved!.HorizonMonths);
    }

    [Theory, Trait("Scenario", "UC14-S11")]
    [InlineData("2026-09-15", 1, 2000)]
    [InlineData("2026-11-02", 0, 0)]
    public async Task S11_PochiDati(string opening, int months, decimal income)
    {
        var openedOn = DateOnly.ParseExact(opening, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        TodayIs(openedOn);
        var intesa = await GivenAccountAsync("Conto Intesa", 0m);
        var stipendio = await GivenCategoryAsync("Stipendio", "Income");
        TodayIs(ScenarioToday);
        for (var month = new DateOnly(openedOn.Year, openedOn.Month, 1).AddMonths(1); month < new DateOnly(2026, 11, 1); month = month.AddMonths(1))
        {
            await GivenIncomeAsync(2000m, intesa, stipendio, month.AddDays(9));
        }

        var projection = await ProjectionAsync();

        Assert.Equal((months, income), (projection.MonthsOfData, projection.Suggested.Income));
    }

    [Fact, Trait("Scenario", "UC14-S12")]
    public async Task S12_RateSalvateEDebitiNuovi()
    {
        await RestartWithAsync(RoomForAll);
        var intesa = await GivenAccountAsync("Conto Intesa", 3000m);
        var auto = await GivenDebtAsync("Auto", intesa, 4000m);
        var body = ValidBody();
        body["payments"] = new[] { new { debtId = auto.Id, amount = 300m } };
        await ReadAsync<SavedProjectionView>(await Mario.PutAsJsonAsync("/api/projection", body), HttpStatusCode.OK);

        var moto = await ReadAsync<DebtView>(
            await Mario.PostAsJsonAsync("/api/debts", new { kind = "Loan", name = "Moto", accountId = intesa.Id, residual = 2000m, monthlyPayment = 150m }),
            HttpStatusCode.Created);
        var projection = await ProjectionAsync();

        Assert.Equal(new ProjectionPaymentView(auto.Id, 300m), Assert.Single(projection.Saved!.Payments));
        Assert.Contains(new ProjectionPaymentView(moto.Id, 150m), projection.Suggested.Payments);
        Assert.Contains(new ProjectionDebtView(moto.Id, "Moto", 2000m, 150m), projection.Debts);
    }

    [Fact]
    public async Task UnPagamentoPerUnDebitoDiUnAltroUtente_ERifiutato()
    {
        var anna = UserClient("Anna");
        var annas = await GivenAccountAsync("Conto Anna", 100m, user: anna);
        var annasDebt = await ReadAsync<DebtView>(await anna.PostAsJsonAsync("/api/debts", DebtBody("Auto", annas.Id, 500m)), HttpStatusCode.Created);
        await GivenAccountAsync("Conto Intesa", 3000m);
        var body = ValidBody();
        body["payments"] = new[] { new { debtId = annasDebt.Id, amount = 300m } };

        var errors = await ErrorsAsync(await Mario.PutAsJsonAsync("/api/projection", body));

        Assert.Equal("InvalidValue", errors["payments[0].debtId"]);
    }

    private async Task TransferAsync(decimal amount, AccountView from, AccountView to, DateOnly date) =>
        await ReadAsync<object>(
            await RecordAsync(new { kind = "Transfer", date, amount, accountId = from.Id, counterAccountId = to.Id }),
            HttpStatusCode.Created);
}
