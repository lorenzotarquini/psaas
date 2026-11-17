using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Categories;
using FinHorizon.MySync.Application.Features.Dashboard;
using FinHorizon.MySync.Application.Features.Debts;
using FinHorizon.MySync.Application.Features.NetWorth;
using FinHorizon.MySync.Application.Features.Transactions;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests.Infrastruttura;

/// <summary>
/// Base of every integration scenario: empty database, fresh backend, the user "Mario" signed in,
/// and the reusable steps of the Gherkin scenarios in docs/mysync/use-cases.
/// </summary>
[Collection(BackendScenarios.Name)]
public abstract class AcceptanceTest(PostgresFixture database) : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private MySyncHost? _host;

    protected MySyncHost Host => _host ?? throw new InvalidOperationException("Scenario not initialized");

    /// <summary>"Mario", the main user of the scenarios.</summary>
    protected HttpClient Mario { get; private set; } = null!;

    protected DateOnly Today => DateOnly.FromDateTime(Host.Clock.GetUtcNow().UtcDateTime);

    public async Task InitializeAsync()
    {
        await database.ResetAsync();
        _host = new MySyncHost(database.ConnectionString);
        Mario = UserClient("Mario");
    }

    /// <summary>"Dato che MySync:MaxDebts vale ...": restarts the backend with other settings, keeping the data and the date.</summary>
    protected async Task RestartWithAsync(IReadOnlyDictionary<string, string> settings)
    {
        var now = Host.Clock.GetUtcNow();
        await Host.DisposeAsync();
        _host = new MySyncHost(database.ConnectionString, settings);
        Host.Clock.Set(now);
        Mario = UserClient("Mario");
    }

    public async Task DisposeAsync() => await Host.DisposeAsync();

    protected HttpClient Anonymous() => Host.CreateClient();

    /// <summary>
    /// A MySync user. Subscribed unless told otherwise: the scenarios of the other use cases write freely,
    /// UC17 builds the expired and failing users it needs.
    /// </summary>
    protected HttpClient UserClient(string name, bool subscribed = true)
    {
        if (subscribed)
        {
            GivenActiveMembership(name);
        }

        return ClientWith(KeycloakTokens.EndUser(name));
    }

    /// <summary>The Stripe customer of a user subscribed by <see cref="UserClient"/>.</summary>
    protected static string CustomerOf(string name) => $"cus_seed_{name.ToLowerInvariant()}";

    /// <summary>"Dato che ... è abbonato": the copy of an active subscription, as the webhook would have left it.</summary>
    private void GivenActiveMembership(string name)
    {
        using var connection = new Npgsql.NpgsqlConnection(database.ConnectionString);
        connection.Open();
        using var command = new Npgsql.NpgsqlCommand(
            """
            INSERT INTO "Memberships" ("OwnerId", "CustomerId", "Status", "CancelAtPeriodEnd", "UpdatedAt")
            VALUES (@owner, @customer, 'Active', false, now()) ON CONFLICT DO NOTHING
            """,
            connection);
        command.Parameters.AddWithValue("owner", $"kc-{name.ToLowerInvariant()}");
        command.Parameters.AddWithValue("customer", CustomerOf(name));
        command.ExecuteNonQuery();
    }

    protected HttpClient ClientWith(string token)
    {
        var client = Host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Moves "today" to the given date, keeping the time of day.</summary>
    protected void TodayIs(DateOnly date) => Host.Clock.Set(new DateTimeOffset(date.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero));

    // ---- Steps: accounts, debts, categories ----

    protected static object AccountBody(string name, decimal initialBalance, string kind = "CurrentAccount", string? description = null, string? tag = null) =>
        new { kind, name, description, tag, initialBalance };

    protected async Task<AccountView> GivenAccountAsync(string name, decimal initialBalance, string kind = "CurrentAccount", HttpClient? user = null) =>
        await ReadAsync<AccountView>(await (user ?? Mario).PostAsJsonAsync("/api/accounts", AccountBody(name, initialBalance, kind)), HttpStatusCode.Created);

    protected static object DebtBody(string name, Guid? accountId, decimal? residual, string kind = "Loan") =>
        new { kind, name, accountId, residual };

    protected async Task<DebtView> GivenDebtAsync(string name, AccountView account, decimal residual, string kind = "Loan") =>
        await ReadAsync<DebtView>(await Mario.PostAsJsonAsync("/api/debts", DebtBody(name, account.Id, residual, kind)), HttpStatusCode.Created);

    protected async Task<CategoryView> GivenCategoryAsync(string name, string kind = "Expense", HttpClient? user = null)
    {
        var response = await (user ?? Mario).PostAsJsonAsync("/api/categories", new { kind, name });
        return await ReadAsync<CategoryView>(response, response.StatusCode == HttpStatusCode.OK ? HttpStatusCode.OK : HttpStatusCode.Created);
    }

    // ---- Steps: movements ----

    protected static object MovementBody(string kind, decimal? amount, AccountView account, CategoryView category, DateOnly? date, DebtView? debt = null, string? note = null) =>
        new { kind, date, amount, accountId = account.Id, categoryId = category.Id, debtId = debt?.Id, note };

    protected Task<HttpResponseMessage> RecordAsync(object body, HttpClient? user = null) => (user ?? Mario).PostAsJsonAsync("/api/transactions", body);

    protected async Task<TransactionView> GivenExpenseAsync(decimal amount, AccountView account, CategoryView category, DebtView? debt = null, DateOnly? date = null) =>
        await ReadAsync<TransactionView>(await RecordAsync(MovementBody("Expense", amount, account, category, date ?? Today, debt)), HttpStatusCode.Created);

    protected async Task<TransactionView> GivenIncomeAsync(decimal amount, AccountView account, CategoryView category, DateOnly? date = null) =>
        await ReadAsync<TransactionView>(await RecordAsync(MovementBody("Income", amount, account, category, date ?? Today)), HttpStatusCode.Created);

    // ---- Steps: reading ----

    protected Task<DashboardView> DashboardAsync(HttpClient? user = null) => GetOkAsync<DashboardView>("/api/dashboard", user);

    protected async Task<AccountView> AccountAsync(Guid id) =>
        (await DashboardAsync()).Accounts.Single(account => account.Id == id);

    protected async Task<DebtView> DebtAsync(Guid id) =>
        (await DashboardAsync()).Debts.Single(debt => debt.Id == id);

    protected Task<NetWorthTrendView> NetWorthAsync(string period) => GetOkAsync<NetWorthTrendView>($"/api/net-worth?period={period}");

    protected Task<TransactionPage> TransactionsAsync(string query = "", HttpClient? user = null) => GetOkAsync<TransactionPage>($"/api/transactions{query}", user);

    // ---- Steps: HTTP ----

    protected async Task<T> GetOkAsync<T>(string url, HttpClient? user = null) => await ReadAsync<T>(await (user ?? Mario).GetAsync(url), HttpStatusCode.OK);

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {(int)expected} {expected}, got {(int)response.StatusCode}: {body}");
        return JsonSerializer.Deserialize<T>(body, Json)!;
    }

    /// <summary>The 400 field errors as field → error code.</summary>
    protected static async Task<Dictionary<string, string>> ErrorsAsync(HttpResponseMessage response)
    {
        var problem = await ReadAsync<JsonElement>(response, HttpStatusCode.BadRequest);
        return problem.GetProperty("errors").EnumerateObject().ToDictionary(error => error.Name, error => error.Value[0].GetString()!);
    }
}
