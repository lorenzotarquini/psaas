using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Debts;
using FinHorizon.MySync.Application.Features.Preferences;
using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.NetWorth;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC13-valute-e-moneta-virtuale.md (S13–S16 are frontend scenarios).</summary>
public sealed class UC13_Valute(PostgresFixture database) : AcceptanceTest(database)
{
    private static readonly Dictionary<string, string> FiveAccounts = new() { ["MySync:MaxAccounts"] = "5", ["MySync:MaxDebts"] = "2" };

    [Fact]
    public async Task S1_ContoInMonetaVirtuale()
    {
        var ledger = await CreateAsync("Ledger", 0.12345678m, "BTC", "VirtualCurrency");

        Assert.Equal(("BTC", 0.12345678m, PortfolioGroup.Investments), (ledger.Currency, ledger.Balance, ledger.Group));
    }

    [Theory]
    [InlineData("CurrentAccount", "BTC")]
    [InlineData("VirtualCurrency", "EUR")]
    [InlineData("CurrentAccount", "XYZ")]
    public async Task S2_ValutaCoerenteConIlTipo(string kind, string currency)
    {
        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/accounts", new { kind, name = "Conto", initialBalance = 1m, currency }));

        Assert.Equal("InvalidValue", errors["currency"]);
    }

    [Fact]
    public async Task S3_PatrimonioNellaValutaDiVisualizzazione()
    {
        await GivenThreeCurrenciesAsync();

        var dashboard = await DashboardAsync();

        Assert.Equal(("EUR", 27_000m), (dashboard.Currency, dashboard.NetWorth));
        var chase = dashboard.Accounts.Single(account => account.Name == "Chase");
        Assert.Equal((1100m, "USD", 1000m), (chase.Balance, chase.Currency, chase.DisplayBalance));
    }

    [Fact]
    public async Task S4_CambioValutaDiVisualizzazione()
    {
        await GivenThreeCurrenciesAsync();

        await ReadAsync<PreferencesView>(await Mario.PutAsJsonAsync("/api/preferences", new { displayCurrency = "USD" }), HttpStatusCode.OK);

        var dashboard = await DashboardAsync();
        Assert.Equal(("USD", 29_700m), (dashboard.Currency, dashboard.NetWorth));
        Assert.Equal("USD", (await NetWorthAsync("30d")).Currency);
    }

    [Fact]
    public async Task S5_AndamentoConIlTassoDelGiorno()
    {
        TodayIs(new DateOnly(2026, 11, 13));
        await CreateAsync("Ledger", 1m, "BTC", "VirtualCurrency");
        TodayIs(new DateOnly(2026, 11, 15));
        Host.Currencies.DayRates.Add(new DailyRate("BTC", new DateOnly(2026, 11, 13), 40_000m));
        Host.Currencies.DayRates.Add(new DailyRate("BTC", new DateOnly(2026, 11, 14), 50_000m));

        var trend = await NetWorthAsync("30d");

        Assert.Equal(40_000m, trend.Points.Single(point => point.Date == new DateOnly(2026, 11, 13)).NetWorth);
        Assert.Equal(50_000m, trend.Points.Single(point => point.Date == new DateOnly(2026, 11, 15)).NetWorth);
    }

    [Fact]
    public async Task S6_TrasferimentoTraValuteDiverse()
    {
        var credem = await GivenAccountAsync("Credem", 1000m);
        var chase = await CreateAsync("Chase", 0m, "USD");

        var withoutReceived = await ErrorsAsync(await TransferAsync(credem, chase, received: null));
        var transfer = await ReadAsync<TransactionView>(await TransferAsync(credem, chase, received: 108m), HttpStatusCode.Created);

        Assert.Equal("Required", withoutReceived["counterAmount"]);
        Assert.Equal((100m, "EUR", 108m, "USD"), (transfer.Amount, transfer.Currency, transfer.CounterAmount, transfer.CounterCurrency));
        Assert.Equal((900m, 108m), ((await AccountAsync(credem.Id)).Balance, (await AccountAsync(chase.Id)).Balance));
    }

    [Fact]
    public async Task S7_RataNellaValutaDelDebito()
    {
        await RestartWithAsync(FiveAccounts);
        var credem = await GivenAccountAsync("Credem", 1000m);
        var chase = await CreateAsync("Chase", 500m, "USD");
        var prestito = await GivenDebtAsync("Prestito", credem, 300m);
        var category = await GivenCategoryAsync("Rate");

        var errors = await ErrorsAsync(await RecordAsync(MovementBody("Expense", 10m, chase, category, Today, prestito)));

        Assert.Equal("EUR", prestito.Currency);
        Assert.Equal("InvalidValue", errors["accountId"]);
    }

    [Fact]
    public async Task S8_ValutaBloccataDaiMovimenti()
    {
        var credem = await GivenAccountAsync("Credem", 1000m);
        await GivenExpenseAsync(10m, credem, await GivenCategoryAsync("Spesa"));

        var errors = await ErrorsAsync(await Mario.PutAsJsonAsync(
            $"/api/accounts/{credem.Id}",
            new { kind = "CurrentAccount", name = "Credem", initialBalance = 1000m, currency = "USD" }));

        Assert.Equal("InvalidValue", errors["currency"]);
    }

    [Fact]
    public async Task S9_CatalogoDelleValute()
    {
        var catalog = await GetOkAsync<List<CurrencyInfo>>("/api/currencies");

        Assert.Equal(CurrencyType.Fiat, catalog.Single(currency => currency.Code == "EUR").Type);
        Assert.Equal(CurrencyType.Fiat, catalog.Single(currency => currency.Code == "USD").Type);
        Assert.Equal(CurrencyType.Crypto, catalog.Single(currency => currency.Code == "BTC").Type);
    }

    [Fact]
    public async Task S10_TassiNonDisponibili()
    {
        await GivenAccountAsync("Credem", 1000m);
        await CreateAsync("Chase", 10m, "USD");
        await RestartWithAsync(FiveAccounts);
        Host.Currencies.Unavailable = true;
        await ReadAsync<JsonElement>(await Mario.GetAsync("/api/transactions"), HttpStatusCode.OK);

        var response = await Mario.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("currency-service-unavailable", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("type").GetString());
    }

    [Fact]
    public async Task S10_TassiNonDisponibili_SoloEuroFunziona()
    {
        await GivenAccountAsync("Credem", 1000m);
        Host.Currencies.Unavailable = true;

        Assert.Equal(1000m, (await DashboardAsync()).NetWorth);
    }

    [Fact]
    public async Task S11_ValutaDiVisualizzazione()
    {
        Assert.Equal(new PreferencesView("EUR", false), await GetOkAsync<PreferencesView>("/api/preferences"));

        var errors = await ErrorsAsync(await Mario.PutAsJsonAsync("/api/preferences", new { displayCurrency = "BTC" }));

        Assert.Equal("InvalidValue", errors["displayCurrency"]);
    }

    [Fact]
    public async Task S12_DecimaliDellaValuta()
    {
        var ledger = await CreateAsync("Ledger", 0m, "BTC", "VirtualCurrency");
        var credem = await GivenAccountAsync("Credem", 100m);
        var mining = await GivenCategoryAsync("Mining", "Income");
        var spesa = await GivenCategoryAsync("Spesa");

        await ReadAsync<TransactionView>(await RecordAsync(MovementBody("Income", 0.00000001m, ledger, mining, Today)), HttpStatusCode.Created);
        var errors = await ErrorsAsync(await RecordAsync(MovementBody("Expense", 1.005m, credem, spesa, Today)));

        Assert.Equal(0.00000001m, (await AccountAsync(ledger.Id)).Balance);
        Assert.Equal("InvalidFormat", errors["amount"]);
    }

    [Fact]
    public async Task DebitoInDollari()
    {
        var chase = await CreateAsync("Chase", 500m, "USD");

        var debt = await ReadAsync<DebtView>(
            await Mario.PostAsJsonAsync("/api/debts", new { kind = "Loan", name = "Car", accountId = chase.Id, residual = 220m, currency = "USD" }),
            HttpStatusCode.Created);

        Assert.Equal("USD", debt.Currency);
        Assert.Equal(200m, (await DashboardAsync()).DebtsResidual);
    }

    private async Task<AccountView> CreateAsync(string name, decimal balance, string currency, string kind = "CurrentAccount") =>
        await ReadAsync<AccountView>(
            await Mario.PostAsJsonAsync("/api/accounts", new { kind, name, initialBalance = balance, currency }),
            HttpStatusCode.Created);

    private async Task GivenThreeCurrenciesAsync()
    {
        await RestartWithAsync(FiveAccounts);
        await GivenAccountAsync("Credem", 1000m);
        await CreateAsync("Chase", 1100m, "USD");
        await CreateAsync("Ledger", 0.5m, "BTC", "VirtualCurrency");
    }

    private Task<HttpResponseMessage> TransferAsync(AccountView from, AccountView to, decimal? received) =>
        RecordAsync(new { kind = "Transfer", date = Today, amount = 100m, accountId = from.Id, counterAccountId = to.Id, counterAmount = received });
}
