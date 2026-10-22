using System.Net;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC2-consultare-cliente.md</summary>
public sealed class UC2_ConsultareCliente(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC2-S1")]
    public async Task S1_SchedaConAnagraficaEOrganizzazione()
    {
        var alfa = await GivenTenantAsync(
            "Alfa Srl",
            profiles: new[] { new { key = "op", name = "Operatore" } },
            productiveUnits: new object[]
            {
                new { name = "Stabilimento", profileKeys = new[] { "op" } },
                new { name = "Magazzino", profileKeys = Array.Empty<string>() }
            });

        var scheda = await TenantAsync(alfa.Id);

        Assert.Equal(new AddressView("Via Roma 1", "Roma", "00118", "IT"), scheda.Address);
        Assert.Equal(["Operatore"], scheda.Profiles.Select(profile => profile.Name));
        Assert.Equal(["Magazzino", "Stabilimento"], scheda.ProductiveUnits.Select(unit => unit.Name).Order());
        Assert.Equal([scheda.Profile("Operatore").Id], scheda.Unit("Stabilimento").ProfileIds);
        Assert.Empty(scheda.Unit("Magazzino").ProfileIds);
    }

    [Fact, Trait("Scenario", "UC2-S2")]
    public async Task S2_ClienteSenzaOrganizzazione()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");

        var scheda = await TenantAsync(alfa.Id);

        Assert.Equal("Alfa Srl", scheda.CompanyName);
        Assert.Empty(scheda.Profiles);
        Assert.Empty(scheda.ProductiveUnits);
    }

    [Fact, Trait("Scenario", "UC2-S3")]
    public async Task S3_ClienteInesistente()
    {
        var response = await Admin.GetAsync($"/api/tenants/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact, Trait("Scenario", "UC2-S4")]
    public async Task S4_UtentiPerProfilo()
    {
        var alfa = await GivenTenantAsync(
            "Alfa Srl",
            profiles: new[] { new { key = "op", name = "Operatore" }, new { key = "rev", name = "Revisore" } });
        KeycloakUsers.Add("Mario", "Rossi", "mario.rossi@example.com", alfa.Profile("Operatore").Id);
        KeycloakUsers.Add("Anna", "Bianchi", "anna.bianchi@example.com", alfa.Profile("Operatore").Id);

        var scheda = await TenantAsync(alfa.Id);

        Assert.Equal(2, scheda.Profile("Operatore").UserCount);
        Assert.Equal(0, scheda.Profile("Revisore").UserCount);
    }

    [Fact, Trait("Scenario", "UC2-S5")]
    public async Task S5_SchedaSenzaKeycloak()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        KeycloakUsers.Unavailable = true;

        var scheda = await TenantAsync(alfa.Id);

        Assert.Null(scheda.Profile("Operatore").UserCount);
    }
}
