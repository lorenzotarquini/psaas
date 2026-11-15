using System.Net;
using System.Net.Http.Json;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>Access rules common to every MySync endpoint (docs/mysync/use-cases/README.md, GEN-S1..S5).</summary>
public sealed class GEN_Sicurezza(PostgresFixture database) : AcceptanceTest(database)
{
    public static TheoryData<string, string> Operations => new()
    {
        { "GET", "/api/me" },
        { "GET", "/api/accounts" },
        { "POST", "/api/accounts" },
        { "GET", "/api/debts" },
        { "POST", "/api/debts" },
        { "PUT", $"/api/accounts/{Guid.Empty}" },
        { "PUT", $"/api/debts/{Guid.Empty}" },
        { "GET", "/api/tags" },
        { "GET", "/api/categories" },
        { "POST", "/api/categories" },
        { "GET", "/api/transactions" },
        { "POST", "/api/transactions" },
        { "GET", $"/api/transactions/{Guid.Empty}" },
        { "PUT", $"/api/transactions/{Guid.Empty}" },
        { "DELETE", $"/api/transactions/{Guid.Empty}" },
        { "GET", "/api/dashboard" },
        { "GET", "/api/net-worth" }
    };

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task S1_SenzaAutenticazioneLAccessoENegato(string method, string url)
    {
        var response = await Anonymous().SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task S2_UnTokenPerUnAltroClientNonVale()
    {
        var response = await ClientWith(KeycloakTokens.Create("Mario", [KeycloakTokens.EndUserRole], audience: "adminportal-api")).GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task S3_OgniOperazioneRichiedeIlRuoloEnduser(string method, string url)
    {
        var response = await ClientWith(KeycloakTokens.Create("Mario", ["systemadministrator"])).SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task S4_LoStatoDelServizioEPubblico()
    {
        var response = await Anonymous().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task S5_DatiDiUnAltroUtenteInvisibili()
    {
        var marios = await GivenAccountAsync("Conto Intesa", 1000m);
        var anna = UserClient("Anna");
        var annasCategory = await GivenCategoryAsync("Spesa", user: anna);

        var annasAccounts = await GetOkAsync<AccountList>("/api/accounts", anna);
        var response = await RecordAsync(MovementBody("Expense", 10m, marios, annasCategory, Today), anna);

        Assert.Empty(annasAccounts.Items);
        Assert.Equal("InvalidValue", (await ErrorsAsync(response))["accountId"]);
        Assert.Equal(1000m, (await AccountAsync(marios.Id)).Balance);
    }

    [Fact]
    public async Task S6_LIdentitaArrivaDalToken()
    {
        var me = await GetOkAsync<Me>("/api/me");

        Assert.Equal("kc-mario", me.KeycloakId);
        Assert.Equal("mario@example.com", me.Email);
        Assert.Contains(KeycloakTokens.EndUserRole, me.Roles);
    }

    private static HttpRequestMessage Request(string method, string url) =>
        new(new HttpMethod(method), url) { Content = method is "POST" or "PUT" ? JsonContent.Create(new { }) : null };

    private sealed record Me(string? KeycloakId, string? Name, string? Email, List<string> Roles);
}
