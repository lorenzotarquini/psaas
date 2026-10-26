using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/README.md – Scenari comuni.</summary>
public sealed class GEN_Sicurezza(PostgresFixture database) : AcceptanceTest(database)
{
    public static TheoryData<string, string> Operazioni => new()
    {
        { "GET", "/api/tenants" },
        { "GET", $"/api/tenants/{Guid.NewGuid()}" },
        { "POST", "/api/tenants" },
        { "PUT", $"/api/tenants/{Guid.NewGuid()}" },
        { "PUT", $"/api/tenants/{Guid.NewGuid()}/organization" },
        { "POST", "/api/users" },
        { "GET", "/api/users" },
        { "GET", "/api/users/kc-1" },
        { "PUT", "/api/users/kc-1" },
        { "POST", "/api/users/kc-1/invite" },
        { "POST", "/api/users/kc-1/disable" },
        { "POST", "/api/users/import" },
        { "GET", "/api/me" },
        { "GET", "/api/profiles" },
        { "GET", "/api/geography/cities?term=ro" },
        { "GET", "/api/tenants/field-validation?field=companyName&value=x" },
        { "GET", "/api/audit" },
        { "POST", $"/api/tenants/{Guid.NewGuid()}/status" },
        { "DELETE", $"/api/tenants/{Guid.NewGuid()}" },
        { "GET", "/api/tenants/counts" },
        { "GET", "/api/tenants/export" },
        { "GET", "/api/saved-views?area=tenants" },
        { "POST", "/api/saved-views" },
        { "GET", "/api/invoices" },
        { "GET", "/api/invoices/summary" },
        { "POST", $"/api/invoices/{Guid.NewGuid()}/payments" },
        { "POST", "/api/billing/run" },
        { "GET", "/api/modules" },
        { "POST", "/api/modules" },
        { "GET", "/api/plans" },
        { "POST", "/api/plans" },
        { "GET", "/api/subscriptions" },
        { "PUT", $"/api/tenants/{Guid.NewGuid()}/subscription" }
    };

    [Theory, Trait("Scenario", "GEN-S1")]
    [MemberData(nameof(Operazioni))]
    public async Task S1_OgniOperazioneRichiedeAutenticazione(string method, string url)
    {
        var response = await Anonymous().SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory, Trait("Scenario", "GEN-S2")]
    [MemberData(nameof(Operazioni))]
    public async Task S2_OgniOperazioneRichiedeIlRuoloAdministrator(string method, string url)
    {
        var response = await ClientWith(KeycloakTokens.Create(roles: ["User"])).SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact, Trait("Scenario", "GEN-S3")]
    public async Task S3_StatoDelServizioSenzaAutenticazione()
    {
        var response = await Anonymous().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    private static HttpRequestMessage Request(string method, string url) => new(new HttpMethod(method), url)
    {
        Content = method is "POST" or "PUT" ? JsonContent.Create(new { }) : null
    };
}
