using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests.Infrastruttura;

/// <summary>
/// Base of every scenario: empty database, fresh backend, an authenticated administrator,
/// and the reusable steps of the Gherkin scenarios in docs/use-cases.
/// </summary>
[Collection(BackendCollection.Name)]
public abstract class AcceptanceTest(PostgresFixture database) : IAsyncLifetime
{
    private static int _uniqueCounter;
    private AdminPortalHost? _host;

    protected AdminPortalHost Host => _host ?? throw new InvalidOperationException("Scenario not initialized");

    /// <summary>The authenticated administrator.</summary>
    protected HttpClient Admin { get; private set; } = null!;

    protected FakeKeycloakUsers KeycloakUsers => Host.KeycloakUsers;

    protected FakeItalianCityList CityList => Host.CityList;

    public async Task InitializeAsync()
    {
        await database.ResetAsync();
        _host = new AdminPortalHost(database.ConnectionString);
        Admin = ClientWith(KeycloakTokens.Administrator());
    }

    public async Task DisposeAsync() => await Host.DisposeAsync();

    protected HttpClient Anonymous() => Host.CreateClient();

    protected HttpClient ClientWith(string token)
    {
        var client = Host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ---- Steps: clients ----

    /// <summary>A valid tenant body; unique values are generated unless given.</summary>
    protected static Dictionary<string, object?> TenantBody(
        string companyName,
        string? completeVatNumber = null,
        string? certifiedEmail = null,
        string street = "Via Roma 1",
        string city = "Roma",
        string postalCode = "00118")
    {
        var unique = Interlocked.Increment(ref _uniqueCounter);
        return new Dictionary<string, object?>
        {
            ["companyName"] = companyName,
            ["completeVatNumber"] = completeVatNumber ?? $"IT{10_000_000_000L + unique}",
            ["certifiedEmail"] = certifiedEmail ?? $"cliente{unique}@pec.it",
            ["street"] = street,
            ["city"] = city,
            ["postalCode"] = postalCode
        };
    }

    protected Task<HttpResponseMessage> ActivateAsync(object body) => Admin.PostAsJsonAsync("/api/tenants", body);

    /// <summary>"Dato che esiste il cliente ...": created through the API, as an administrator would.</summary>
    protected async Task<TenantDetail> GivenTenantAsync(
        string companyName,
        string? completeVatNumber = null,
        string? certifiedEmail = null,
        string city = "Roma",
        string postalCode = "00118",
        object? profiles = null,
        object? productiveUnits = null)
    {
        var body = TenantBody(companyName, completeVatNumber, certifiedEmail, city: city, postalCode: postalCode);
        body["profiles"] = profiles;
        body["productiveUnits"] = productiveUnits;
        return await ReadAsync<TenantDetail>(await ActivateAsync(body), HttpStatusCode.Created);
    }

    protected Task<TenantDetail> TenantAsync(Guid id) => GetOkAsync<TenantDetail>($"/api/tenants/{id}");

    protected async Task<PageView<TenantSummary>> TenantsAsync(string query = "") =>
        await GetOkAsync<PageView<TenantSummary>>($"/api/tenants{query}");

    // ---- Steps: catalog ----

    protected async Task<ModuleView> GivenModuleAsync(string id, string version = "1.0.0", params string[] requires) =>
        await ReadAsync<ModuleView>(
            await Admin.PostAsJsonAsync("/api/modules", new { id, name = id, version, requires, permissions = new[] { $"{id}.read" } }),
            HttpStatusCode.OK);

    protected async Task<PlanView> GivenPlanAsync(string code, decimal price = 99, int? maxUsers = null, int trialDays = 0, params string[] moduleIds) =>
        await ReadAsync<PlanView>(
            await Admin.PostAsJsonAsync("/api/plans", new { code, name = code.ToUpperInvariant(), price, billingPeriod = "Monthly", trialDays, maxUsers, moduleIds }),
            HttpStatusCode.Created);

    protected Task<HttpResponseMessage> SubscribeAsync(Guid tenantId, Guid planId, bool trial = false) =>
        Admin.PutAsJsonAsync($"/api/tenants/{tenantId}/subscription", new { planId, trial });

    // ---- Steps: HTTP ----

    protected async Task<T> GetOkAsync<T>(string url) => await ReadAsync<T>(await Admin.GetAsync(url), HttpStatusCode.OK);

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {(int)expected} {expected}, got {(int)response.StatusCode}: {body}");
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    /// <summary>The 400 field errors as field path → error code.</summary>
    protected static async Task<Dictionary<string, string>> ErrorsAsync(HttpResponseMessage response)
    {
        var problem = await ReadAsync<JsonElement>(response, HttpStatusCode.BadRequest);
        return problem.GetProperty("errors").EnumerateObject()
            .ToDictionary(error => error.Name, error => error.Value[0].GetString()!);
    }
}
