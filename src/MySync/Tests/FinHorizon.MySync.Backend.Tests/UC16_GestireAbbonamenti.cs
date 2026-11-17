using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Categories;
using FinHorizon.MySync.Application.Features.Subscriptions;
using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC16-gestire-abbonamenti.md</summary>
public sealed class UC16_GestireAbbonamenti(PostgresFixture database) : AcceptanceTest(database)
{
    private static readonly DateOnly ScenarioToday = new(2026, 11, 17);

    private static readonly Dictionary<string, string> RoomForAll = new() { ["MySync:MaxAccounts"] = "5" };

    private AccountView _intesa = null!;
    private CategoryView _abbonamenti = null!;

    private static DateOnly Date(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>The background of every scenario: "Conto Intesa" opened on 2026-01-10 with 2,000.00 € and the category "Abbonamenti".</summary>
    private async Task GivenMarioAsync(IReadOnlyDictionary<string, string>? settings = null)
    {
        if (settings is not null)
        {
            await RestartWithAsync(settings);
        }

        TodayIs(new DateOnly(2026, 1, 10));
        _intesa = await GivenAccountAsync("Conto Intesa", 2000m);
        _abbonamenti = await GivenCategoryAsync("Abbonamenti");
        TodayIs(ScenarioToday);
    }

    private static object Body(string? name, Guid? accountId, Guid? categoryId, decimal? amount, DateOnly? firstChargeOn, string? description = null) =>
        new { name, description, accountId, categoryId, amount, firstChargeOn };

    private Task<HttpResponseMessage> CreateAsync(object body, HttpClient? user = null) => (user ?? Mario).PostAsJsonAsync("/api/subscriptions", body);

    private async Task<SubscriptionView> GivenSubscriptionAsync(string name, decimal amount, string firstChargeOn, HttpClient? user = null, AccountView? account = null, CategoryView? category = null) =>
        await ReadAsync<SubscriptionView>(
            await CreateAsync(Body(name, (account ?? _intesa).Id, (category ?? _abbonamenti).Id, amount, Date(firstChargeOn)), user),
            HttpStatusCode.Created);

    private Task<SubscriptionList> ListAsync(HttpClient? user = null) => GetOkAsync<SubscriptionList>("/api/subscriptions", user);

    private async Task<SubscriptionView> SubscriptionAsync(Guid id, HttpClient? user = null) =>
        (await ListAsync(user)).Items.Single(item => item.Id == id);

    private Task<HttpResponseMessage> PayAsync(SubscriptionView subscription, decimal amount, string date, CategoryView? category = null, AccountView? account = null, string kind = "Expense", Guid? debtId = null) =>
        RecordAsync(new
        {
            kind,
            date = Date(date),
            amount,
            accountId = (account ?? _intesa).Id,
            categoryId = (category ?? _abbonamenti).Id,
            debtId,
            subscriptionId = subscription.Id,
        });

    private async Task<TransactionView> GivenPaymentAsync(SubscriptionView subscription, decimal amount, string date, CategoryView? category = null) =>
        await ReadAsync<TransactionView>(await PayAsync(subscription, amount, date, category), HttpStatusCode.Created);

    private Task<HttpResponseMessage> CancelAsync(Guid id, string? endsOn, HttpClient? user = null) =>
        (user ?? Mario).PostAsJsonAsync($"/api/subscriptions/{id}/cancel", new { endsOn = endsOn is null ? (DateOnly?)null : Date(endsOn) });

    /// <summary>UC16-S4 "Dato": "Netflix" created on 2026-10-01, first charge 2026-10-05, the October charge registered; today is 2026-11-17.</summary>
    private async Task<SubscriptionView> GivenNetflixOfS4Async()
    {
        TodayIs(new DateOnly(2026, 10, 1));
        var netflix = await GivenSubscriptionAsync("Netflix", 17.99m, "2026-10-05");
        TodayIs(ScenarioToday);
        await GivenPaymentAsync(netflix, 17.99m, "2026-10-05");
        return netflix;
    }

    [Fact, Trait("Scenario", "UC16-S1")]
    public async Task S1_AbbonamentoCreato()
    {
        await GivenMarioAsync();

        var netflix = await GivenSubscriptionAsync("Netflix", 17.99m, "2026-12-05");

        Assert.True(netflix.Active);
        Assert.Equal((Date("2026-12-05"), (DateOnly?)null), (netflix.NextChargeOn, netflix.DueOn));
        Assert.Equal(17.99m, (await ListAsync()).MonthlyTotal);
        Assert.Equal(2000m, (await AccountAsync(_intesa.Id)).Balance);
    }

    public static TheoryData<string, string, string> InvalidData => new()
    {
        { "nome vuoto", "Required", "name" },
        { "nome NETFLIX", "NotUnique", "name" },
        { "importo 0", "InvalidValue", "amount" },
        { "importo 10,001", "InvalidFormat", "amount" },
        { "conto Conto Anna", "InvalidValue", "accountId" },
        { "conto Wallet BTC", "InvalidValue", "accountId" },
        { "categoria Stipendio", "InvalidValue", "categoryId" },
        { "senza primo addebito", "Required", "firstChargeOn" },
    };

    [Theory, Trait("Scenario", "UC16-S2")]
    [MemberData(nameof(InvalidData))]
    public async Task S2_DatiNonValidi(string data, string code, string field)
    {
        await GivenMarioAsync(RoomForAll);
        await GivenSubscriptionAsync("Netflix", 17.99m, "2026-12-05");
        var annas = await GivenAccountAsync("Conto Anna", 100m, user: UserClient("Anna"));
        var stipendio = await GivenCategoryAsync("Stipendio", "Income");
        var wallet = await ReadAsync<AccountView>(
            await Mario.PostAsJsonAsync("/api/accounts", new { kind = "VirtualCurrency", name = "Wallet BTC", initialBalance = 0.1m, currency = "BTC" }),
            HttpStatusCode.Created);
        var body = data switch
        {
            "nome vuoto" => Body("", _intesa.Id, _abbonamenti.Id, 9.99m, ScenarioToday),
            "nome NETFLIX" => Body("NETFLIX", _intesa.Id, _abbonamenti.Id, 9.99m, ScenarioToday),
            "importo 0" => Body("Spotify", _intesa.Id, _abbonamenti.Id, 0m, ScenarioToday),
            "importo 10,001" => Body("Spotify", _intesa.Id, _abbonamenti.Id, 10.001m, ScenarioToday),
            "conto Conto Anna" => Body("Spotify", annas.Id, _abbonamenti.Id, 9.99m, ScenarioToday),
            "conto Wallet BTC" => Body("Spotify", wallet.Id, _abbonamenti.Id, 9.99m, ScenarioToday),
            "categoria Stipendio" => Body("Spotify", _intesa.Id, stipendio.Id, 9.99m, ScenarioToday),
            _ => Body("Spotify", _intesa.Id, _abbonamenti.Id, 9.99m, null),
        };

        var errors = await ErrorsAsync(await CreateAsync(body));

        Assert.Equal(code, errors[field]);
        Assert.Single((await ListAsync()).Items);
    }

    [Fact, Trait("Scenario", "UC16-S3")]
    public async Task S3_Limite()
    {
        await GivenMarioAsync(new Dictionary<string, string> { ["MySync:MaxSubscriptions"] = "1" });
        var netflix = await GivenSubscriptionAsync("Netflix", 17.99m, "2026-12-05");
        var spotify = Body("Spotify", _intesa.Id, _abbonamenti.Id, 10.99m, ScenarioToday);

        Assert.Equal("LimitReached", (await ErrorsAsync(await CreateAsync(spotify)))["limit"]);

        await ReadAsync<SubscriptionView>(await CancelAsync(netflix.Id, "2026-12-30"), HttpStatusCode.OK);
        await ReadAsync<SubscriptionView>(await CreateAsync(spotify), HttpStatusCode.Created);

        var resume = await Mario.PostAsync($"/api/subscriptions/{netflix.Id}/resume", null);
        Assert.Equal("LimitReached", (await ErrorsAsync(resume))["limit"]);
        Assert.Equal(1, (await ListAsync()).Max);
    }

    [Fact]
    public async Task S3b_ConZeroGliAbbonamentiSonoDisattivati()
    {
        await GivenMarioAsync(new Dictionary<string, string> { ["MySync:MaxSubscriptions"] = "0" });

        Assert.Equal("LimitReached", (await ErrorsAsync(await CreateAsync(Body("Netflix", _intesa.Id, _abbonamenti.Id, 9.99m, ScenarioToday))))["limit"]);
        Assert.Equal(0, (await ListAsync()).Max);
    }

    [Fact, Trait("Scenario", "UC16-S4")]
    public async Task S4_RegistrareLAddebito()
    {
        await GivenMarioAsync();
        var netflix = await GivenNetflixOfS4Async();

        var due = await SubscriptionAsync(netflix.Id);
        Assert.Equal((Date("2026-11-05"), 17.99m), (due.DueOn!.Value, due.Amount));

        await GivenPaymentAsync(netflix, due.Amount, "2026-11-05");

        Assert.Equal(1964.02m, (await AccountAsync(_intesa.Id)).Balance);
        var paid = await SubscriptionAsync(netflix.Id);
        Assert.Null(paid.DueOn);
        Assert.Equal(Date("2026-12-05"), paid.NextChargeOn);
        Assert.True(paid.HasPayments);
    }

    [Fact, Trait("Scenario", "UC16-S5")]
    public async Task S5_UnaSpesaNelloStessoMeseVale()
    {
        await GivenMarioAsync();
        var svago = await GivenCategoryAsync("Svago");
        var netflix = await GivenNetflixOfS4Async();

        var payment = await GivenPaymentAsync(netflix, 19.99m, "2026-11-09", svago);

        var paid = await SubscriptionAsync(netflix.Id);
        Assert.Null(paid.DueOn);
        Assert.Equal(17.99m, paid.Amount);
        Assert.Equal(("Netflix", "Svago"), (payment.SubscriptionName, payment.CategoryName));

        Assert.Equal(HttpStatusCode.NoContent, (await Mario.DeleteAsync($"/api/transactions/{payment.Id}")).StatusCode);

        Assert.Equal(Date("2026-11-05"), (await SubscriptionAsync(netflix.Id)).DueOn);
    }

    [Theory, Trait("Scenario", "UC16-S6")]
    [InlineData("2026-02-01", "2026-02-28")]
    [InlineData("2026-03-01", "2026-03-31")]
    [InlineData("2026-04-01", "2026-04-30")]
    [InlineData("2028-02-01", "2028-02-29")]
    public async Task S6_GiornoDiAddebito(string today, string charge)
    {
        TodayIs(new DateOnly(2026, 1, 10));
        _intesa = await GivenAccountAsync("Conto Intesa", 2000m);
        _abbonamenti = await GivenCategoryAsync("Abbonamenti");
        var cloud = await GivenSubscriptionAsync("Cloud", 3m, "2026-01-31");

        TodayIs(Date(today));

        Assert.Equal(Date(charge), (await SubscriptionAsync(cloud.Id)).NextChargeOn);
    }

    [Fact, Trait("Scenario", "UC16-S7")]
    public async Task S7_GliAddebitiPrimaDellaCreazione()
    {
        await GivenMarioAsync();
        var oldExpense = await GivenExpenseAsync(45m, _intesa, _abbonamenti, date: Date("2026-10-03"));

        var palestra = await GivenSubscriptionAsync("Palestra", 45m, "2026-09-03");

        Assert.Null(palestra.DueOn);
        Assert.Equal(Date("2026-12-03"), palestra.NextChargeOn);

        var linked = await ReadAsync<TransactionView>(
            await Mario.PutAsJsonAsync($"/api/transactions/{oldExpense.Id}", new
            {
                kind = "Expense",
                date = oldExpense.Date,
                amount = 45m,
                accountId = _intesa.Id,
                categoryId = _abbonamenti.Id,
                subscriptionId = palestra.Id,
            }),
            HttpStatusCode.OK);
        Assert.Equal(palestra.Id, linked.SubscriptionId);
        Assert.Single((await TransactionsAsync($"?subscriptionId={palestra.Id}")).Items);
    }

    [Fact, Trait("Scenario", "UC16-S8")]
    public async Task S8_SaltareUnAddebito()
    {
        await GivenMarioAsync();
        var netflix = await GivenNetflixOfS4Async();
        var balance = (await AccountAsync(_intesa.Id)).Balance;

        var skipped = await ReadAsync<SubscriptionView>(
            await Mario.PostAsJsonAsync($"/api/subscriptions/{netflix.Id}/skip", new { dueOn = Date("2026-11-05") }),
            HttpStatusCode.OK);

        Assert.Null(skipped.DueOn);
        Assert.Equal(balance, (await AccountAsync(_intesa.Id)).Balance);
        TodayIs(new DateOnly(2026, 12, 5));
        Assert.Equal(Date("2026-12-05"), (await SubscriptionAsync(netflix.Id)).DueOn);
    }

    [Fact]
    public async Task S8b_SiSaltaSoloLAddebitoDaRegistrare()
    {
        await GivenMarioAsync();
        var netflix = await GivenNetflixOfS4Async();

        var response = await Mario.PostAsJsonAsync($"/api/subscriptions/{netflix.Id}/skip", new { dueOn = Date("2026-10-05") });

        Assert.Equal("InvalidValue", (await ErrorsAsync(response))["dueOn"]);
    }

    [Fact, Trait("Scenario", "UC16-S9")]
    public async Task S9_Annullare()
    {
        await GivenMarioAsync();
        var netflix = await GivenNetflixOfS4Async();
        await GivenPaymentAsync(netflix, 17.99m, "2026-11-05");

        var cancelled = await ReadAsync<SubscriptionView>(await CancelAsync(netflix.Id, "2026-12-10"), HttpStatusCode.OK);

        Assert.Equal((Date("2026-12-10"), true), (cancelled.EndsOn!.Value, cancelled.Active));
        Assert.Equal(Date("2026-12-05"), cancelled.NextChargeOn);
        Assert.Equal(17.99m, (await ListAsync()).MonthlyTotal);

        TodayIs(new DateOnly(2026, 12, 5));
        await GivenPaymentAsync(netflix, 17.99m, "2026-12-05");
        TodayIs(new DateOnly(2026, 12, 11));
        var list = await ListAsync();
        Assert.Equal(0m, list.MonthlyTotal);
        Assert.False(Assert.Single(list.Items).Active);
        Assert.Null(list.Items[0].NextChargeOn);

        TodayIs(new DateOnly(2027, 1, 5));
        Assert.Null((await SubscriptionAsync(netflix.Id)).DueOn);
    }

    [Fact]
    public async Task S9b_Riattivare()
    {
        await GivenMarioAsync();
        var netflix = await GivenNetflixOfS4Async();
        await ReadAsync<SubscriptionView>(await CancelAsync(netflix.Id, "2026-10-31"), HttpStatusCode.OK);
        Assert.Null((await SubscriptionAsync(netflix.Id)).DueOn);

        var resumed = await ReadAsync<SubscriptionView>(await Mario.PostAsync($"/api/subscriptions/{netflix.Id}/resume", null), HttpStatusCode.OK);

        Assert.Null(resumed.EndsOn);
        Assert.Null(resumed.DueOn);
        Assert.Equal(Date("2026-12-05"), resumed.NextChargeOn);
    }

    [Theory, Trait("Scenario", "UC16-S10")]
    [InlineData("2026-09-30")]
    [InlineData("2026-11-04")]
    public async Task S10_DataDiFineNonValida(string endsOn)
    {
        await GivenMarioAsync();
        var netflix = await GivenNetflixOfS4Async();
        await GivenPaymentAsync(netflix, 17.99m, "2026-11-05");

        var errors = await ErrorsAsync(await CancelAsync(netflix.Id, endsOn));

        Assert.Equal("InvalidValue", errors["endsOn"]);
        Assert.Null((await SubscriptionAsync(netflix.Id)).EndsOn);
    }

    [Theory, Trait("Scenario", "UC16-S11")]
    [InlineData("un ricavo", "subscriptionId")]
    [InlineData("una rata di Auto", "subscriptionId")]
    [InlineData("una spesa da Revolut USD", "accountId")]
    [InlineData("una spesa del 2026-11-12", "date")]
    [InlineData("una spesa del 2026-09-30", "date")]
    public async Task S11_SpeseCollegateNonValide(string movement, string field)
    {
        await GivenMarioAsync();
        TodayIs(new DateOnly(2026, 1, 10));
        var revolut = await ReadAsync<AccountView>(
            await Mario.PostAsJsonAsync("/api/accounts", new { kind = "VirtualBank", name = "Revolut USD", initialBalance = 100m, currency = "USD" }),
            HttpStatusCode.Created);
        var auto = await GivenDebtAsync("Auto", _intesa, 4000m);
        var stipendio = await GivenCategoryAsync("Stipendio", "Income");
        var netflix = await GivenNetflixOfS4Async();
        await ReadAsync<SubscriptionView>(await CancelAsync(netflix.Id, "2026-11-10"), HttpStatusCode.OK);

        var response = movement switch
        {
            "un ricavo" => await PayAsync(netflix, 17.99m, "2026-11-05", stipendio, kind: "Income"),
            "una rata di Auto" => await PayAsync(netflix, 17.99m, "2026-11-05", debtId: auto.Id),
            "una spesa da Revolut USD" => await PayAsync(netflix, 17.99m, "2026-11-05", account: revolut),
            "una spesa del 2026-11-12" => await PayAsync(netflix, 17.99m, "2026-11-12"),
            _ => await PayAsync(netflix, 17.99m, "2026-09-30"),
        };

        Assert.Equal("InvalidValue", (await ErrorsAsync(response))[field]);
    }

    [Fact, Trait("Scenario", "UC16-S12")]
    public async Task S12_Eliminare()
    {
        await GivenMarioAsync();
        var spotify = await GivenSubscriptionAsync("Spotify", 10.99m, "2026-12-01");
        var netflix = await GivenNetflixOfS4Async();

        Assert.Equal(HttpStatusCode.NoContent, (await Mario.DeleteAsync($"/api/subscriptions/{spotify.Id}")).StatusCode);
        Assert.Equal("InvalidValue", (await ErrorsAsync(await Mario.DeleteAsync($"/api/subscriptions/{netflix.Id}")))["payments"]);

        Assert.Equal(["Netflix"], (await ListAsync()).Items.Select(item => item.Name));
    }

    [Fact, Trait("Scenario", "UC16-S13")]
    public async Task S13_ModificareNonCambiaLePassate()
    {
        await GivenMarioAsync();
        var netflix = await GivenNetflixOfS4Async();
        var november = await GivenPaymentAsync(netflix, 17.99m, "2026-11-05");
        var balance = (await AccountAsync(_intesa.Id)).Balance;

        var updated = await ReadAsync<SubscriptionView>(
            await Mario.PutAsJsonAsync($"/api/subscriptions/{netflix.Id}", Body("Netflix", _intesa.Id, _abbonamenti.Id, 19.99m, Date("2026-10-05"))),
            HttpStatusCode.OK);

        Assert.Equal(19.99m, updated.Amount);
        Assert.Equal(19.99m, (await ListAsync()).MonthlyTotal);
        Assert.Equal(17.99m, (await GetOkAsync<TransactionView>($"/api/transactions/{november.Id}")).Amount);
        Assert.Equal(balance, (await AccountAsync(_intesa.Id)).Balance);
    }

    [Fact, Trait("Scenario", "UC16-S14")]
    public async Task S14_AbbonamentoDiUnAltroUtente()
    {
        await GivenMarioAsync();
        var anna = UserClient("Anna");
        var annasAccount = await GivenAccountAsync("Conto Anna", 100m, user: anna);
        var annasCategory = await GivenCategoryAsync("Abbonamenti", user: anna);
        var annas = await GivenSubscriptionAsync("Netflix", 17.99m, "2026-11-01", anna, annasAccount, annasCategory);
        var url = $"/api/subscriptions/{annas.Id}";

        Assert.Equal(HttpStatusCode.NotFound, (await Mario.PutAsJsonAsync(url, Body("Mio", _intesa.Id, _abbonamenti.Id, 1m, ScenarioToday))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await CancelAsync(annas.Id, "2026-11-30")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Mario.PostAsJsonAsync($"{url}/skip", new { dueOn = Date("2026-11-01") })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Mario.PostAsync($"{url}/resume", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Mario.DeleteAsync(url)).StatusCode);
        Assert.Equal("InvalidValue", (await ErrorsAsync(await PayAsync(annas, 17.99m, "2026-11-05")))["subscriptionId"]);

        var unchanged = await SubscriptionAsync(annas.Id, anna);
        Assert.Equal(("Netflix", 17.99m, (DateOnly?)null), (unchanged.Name, unchanged.Amount, unchanged.EndsOn));
        Assert.Empty((await ListAsync()).Items);
    }

    [Fact]
    public async Task ConSpeseCollegateIlContoRestaNellaStessaValuta()
    {
        await GivenMarioAsync();
        TodayIs(new DateOnly(2026, 1, 10));
        var revolut = await ReadAsync<AccountView>(
            await Mario.PostAsJsonAsync("/api/accounts", new { kind = "VirtualBank", name = "Revolut USD", initialBalance = 100m, currency = "USD" }),
            HttpStatusCode.Created);
        var netflix = await GivenNetflixOfS4Async();

        var errors = await ErrorsAsync(await Mario.PutAsJsonAsync(
            $"/api/subscriptions/{netflix.Id}",
            Body("Netflix", revolut.Id, _abbonamenti.Id, 17.99m, Date("2026-10-06"))));

        Assert.Equal(("InvalidValue", "InvalidValue"), (errors["accountId"], errors["firstChargeOn"]));
    }
}
