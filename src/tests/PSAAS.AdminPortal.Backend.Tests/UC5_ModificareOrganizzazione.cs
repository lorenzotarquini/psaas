using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC5-modificare-organizzazione.md</summary>
public sealed class UC5_ModificareOrganizzazione(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC5-S1")]
    public async Task S1_AggiungereProfiloEUnitaCollegati()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");

        var response = await SaveOrganizationAsync(alfa.Id,
            profiles: [new { id = (string?)null, key = "op", name = "Operatore" }],
            units: [new { id = (Guid?)null, name = "Stabilimento", profileKeys = new[] { "op" } }]);

        await ReadAsync<TenantDetail>(response, HttpStatusCode.OK);
        var scheda = await TenantAsync(alfa.Id);
        Assert.Equal([scheda.Profile("Operatore").Id], scheda.Unit("Stabilimento").ProfileIds);
    }

    [Fact, Trait("Scenario", "UC5-S2")]
    public async Task S2_RinominareMantenendoLIdentita()
    {
        var alfa = await GivenTenantWithOperatorInPlantAsync();
        var operatore = alfa.Profile("Operatore");
        var stabilimento = alfa.Unit("Stabilimento");

        await ReadAsync<TenantDetail>(await SaveOrganizationAsync(alfa.Id,
            profiles: [new { id = operatore.Id, key = "p", name = "Addetto" }],
            units: [new { id = stabilimento.Id, name = "Fabbrica", profileKeys = new[] { "p" } }]),
            HttpStatusCode.OK);

        var scheda = await TenantAsync(alfa.Id);
        Assert.Equal(operatore.Id, scheda.Profile("Addetto").Id);
        Assert.Equal(stabilimento.Id, scheda.Unit("Fabbrica").Id);
        Assert.Equal([operatore.Id], scheda.Unit("Fabbrica").ProfileIds);
    }

    [Fact, Trait("Scenario", "UC5-S3")]
    public async Task S3_TogliereUnUnitaLasciaIlProfiloSenzaUnita()
    {
        var alfa = await GivenTenantAsync(
            "Alfa Srl",
            profiles: new[] { new { key = "mag", name = "Magazziniere" } },
            productiveUnits: new[] { new { name = "Magazzino", profileKeys = new[] { "mag" } } });
        var magazziniere = alfa.Profile("Magazziniere");

        await ReadAsync<TenantDetail>(await SaveOrganizationAsync(alfa.Id,
            profiles: [new { id = magazziniere.Id, key = "mag", name = "Magazziniere" }],
            units: []),
            HttpStatusCode.OK);

        var scheda = await TenantAsync(alfa.Id);
        Assert.Equal(magazziniere.Id, Assert.Single(scheda.Profiles).Id);
        Assert.Empty(scheda.ProductiveUnits);
    }

    [Fact, Trait("Scenario", "UC5-S4")]
    public async Task S4_TogliereUnProfiloLoToglieAgliUtenti()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "mag", name = "Magazziniere" } });
        KeycloakUsers.Add("Mario", "Rossi", "mario.rossi@example.com", alfa.Profile("Magazziniere").Id);

        await ReadAsync<TenantDetail>(await SaveOrganizationAsync(alfa.Id, profiles: [], units: []), HttpStatusCode.OK);

        var mario = Assert.Single(await GetOkAsync<List<UserView>>("/api/users?search=Mario"));
        Assert.Empty(mario.Profiles);
    }

    [Fact, Trait("Scenario", "UC5-S5")]
    public async Task S5_NomiDuplicati()
    {
        var alfa = await GivenTenantWithOperatorInPlantAsync();

        var errors = await ErrorsAsync(await SaveOrganizationAsync(alfa.Id,
            profiles:
            [
                new { id = (string?)null, key = "a", name = "Operatore" },
                new { id = (string?)null, key = "b", name = "OPERATORE" }
            ],
            units:
            [
                new { id = (Guid?)null, name = "Magazzino", profileKeys = Array.Empty<string>() },
                new { id = (Guid?)null, name = "Magazzino", profileKeys = Array.Empty<string>() }
            ]));

        Assert.Equal("NotUnique", errors["profiles[1].name"]);
        Assert.Equal("NotUnique", errors["productiveUnits[1].name"]);
        Assert.Equal(alfa.ProductiveUnits.Select(unit => unit.Id), (await TenantAsync(alfa.Id)).ProductiveUnits.Select(unit => unit.Id));
    }

    [Fact, Trait("Scenario", "UC5-S6")]
    public async Task S6_ProfiloOUnitaDiUnAltroCliente()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");
        var beta = await GivenTenantWithOperatorInPlantAsync("Beta Snc");

        var errors = await ErrorsAsync(await SaveOrganizationAsync(alfa.Id,
            profiles: [new { id = beta.Profile("Operatore").Id, key = "x", name = "Rubato" }],
            units: [new { id = (Guid?)beta.Unit("Stabilimento").Id, name = "Rubata", profileKeys = Array.Empty<string>() }]));

        Assert.Equal("InvalidValue", errors["profiles[0].id"]);
        Assert.Equal("InvalidValue", errors["productiveUnits[0].id"]);
        var betaDopo = await TenantAsync(beta.Id);
        Assert.Equal(beta.Profiles, betaDopo.Profiles);
        Assert.Equal(["Stabilimento"], betaDopo.ProductiveUnits.Select(unit => unit.Name));
    }

    [Fact, Trait("Scenario", "UC5-S7")]
    public async Task S7_TogliereTuttaLOrganizzazione()
    {
        var alfa = await GivenTenantWithOperatorInPlantAsync();

        await ReadAsync<TenantDetail>(await SaveOrganizationAsync(alfa.Id, profiles: [], units: []), HttpStatusCode.OK);

        var scheda = await TenantAsync(alfa.Id);
        Assert.Empty(scheda.Profiles);
        Assert.Empty(scheda.ProductiveUnits);
    }

    [Fact, Trait("Scenario", "UC5-S8")]
    public async Task S8_ClienteInesistente()
    {
        var response = await SaveOrganizationAsync(Guid.NewGuid(), profiles: [], units: []);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<TenantDetail> GivenTenantWithOperatorInPlantAsync(string companyName = "Alfa Srl") => GivenTenantAsync(
        companyName,
        profiles: new[] { new { key = "op", name = "Operatore" } },
        productiveUnits: new[] { new { name = "Stabilimento", profileKeys = new[] { "op" } } });

    private Task<HttpResponseMessage> SaveOrganizationAsync(Guid id, object[] profiles, object[] units) =>
        Admin.PutAsJsonAsync($"/api/tenants/{id}/organization", new { profiles, productiveUnits = units });
}
