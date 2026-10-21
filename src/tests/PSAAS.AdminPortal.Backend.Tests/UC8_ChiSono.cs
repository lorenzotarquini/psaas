using System.Net;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC8-chi-sono.md</summary>
public sealed class UC8_ChiSono(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC8-S1")]
    public async Task S1_IdentitaLettaDalToken()
    {
        var me = await GetOkAsync<MeView>("/api/me");

        Assert.Equal(KeycloakTokens.AdminSubject, me.KeycloakId);
        Assert.Equal("admin.psaas", me.Name);
        Assert.Equal("admin@psaas.it", me.Email);
        Assert.Equal(["Administrator"], me.Roles);
    }

    [Fact, Trait("Scenario", "UC8-S2")]
    public async Task S2_SenzaToken()
    {
        var response = await Anonymous().GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact, Trait("Scenario", "UC8-S3")]
    public async Task S3_TokenScaduto()
    {
        var response = await ClientWith(KeycloakTokens.Create(roles: ["Administrator"], expired: true)).GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact, Trait("Scenario", "UC8-S4")]
    public async Task S4_TokenPerUnAltraApplicazione()
    {
        var response = await ClientWith(KeycloakTokens.Create(roles: ["Administrator"], audience: "altra-app")).GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact, Trait("Scenario", "UC8-S5")]
    public async Task S5_SenzaRuoloAdministrator()
    {
        var response = await ClientWith(KeycloakTokens.Create(roles: ["User"])).GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
