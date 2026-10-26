using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC30-ricerca-globale.md</summary>
public sealed class UC30_RicercaGlobale(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC30-S1")]
    public async Task S1_ClientiEUtenti()
    {
        await GivenTenantAsync("Ferri Costr. Srl");
        await GivenTenantAsync("Alfa Srl");
        KeycloakUsers.Add("Giulia", "Ferri", "giulia@example.com");

        var result = await GetOkAsync<SearchView>("/api/search?q=ferri");

        Assert.Equal("Ferri Costr. Srl", Assert.Single(result.Tenants).CompanyName);
        Assert.Equal("Giulia", Assert.Single(result.Users).FirstName);
    }

    [Fact, Trait("Scenario", "UC30-S2")]
    public async Task S2_PerPartitaIva()
    {
        await GivenTenantAsync("Alfa Srl", completeVatNumber: "IT12345678901");

        var result = await GetOkAsync<SearchView>("/api/search?q=12345678");

        Assert.Equal("Alfa Srl", Assert.Single(result.Tenants).CompanyName);
    }

    [Fact, Trait("Scenario", "UC30-S3")]
    public async Task S3_KeycloakGiu()
    {
        await GivenTenantAsync("Alfa Srl");
        KeycloakUsers.Unavailable = true;

        var result = await GetOkAsync<SearchView>("/api/search?q=alfa");

        Assert.Single(result.Tenants);
        Assert.True(result.UsersUnavailable);
    }

    [Fact, Trait("Scenario", "UC30-S4")]
    public async Task S4_TroppoCorto()
    {
        Assert.Equal("InvalidValue", (await ErrorsAsync(await Admin.GetAsync("/api/search?q=a")))["q"]);
    }
}
