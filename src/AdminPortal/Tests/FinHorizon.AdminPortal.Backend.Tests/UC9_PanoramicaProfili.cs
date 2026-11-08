using System.Net;
using System.Net.Http.Json;
using FinHorizon.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC9-panoramica-profili.md</summary>
public sealed class UC9_PanoramicaProfili(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC9-S1")]
    public async Task S1_TuttiIProfiliConIlLoroCliente()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        var beta = await GivenTenantAsync("Beta Snc", profiles: new[] { new { key = "amm", name = "Amministrativo" } });

        var profiles = await GetOkAsync<List<ProfileOverview>>("/api/profiles");

        Assert.Equal(
            [("Amministrativo", beta.Id.ToString(), "Beta Snc"), ("Operatore", alfa.Id.ToString(), "Alfa Srl")],
            profiles.Select(profile => (profile.Name, profile.TenantId, profile.TenantCompanyName)));
    }

    [Fact, Trait("Scenario", "UC9-S2")]
    public async Task S2_NessunProfilo()
    {
        Assert.Empty(await GetOkAsync<List<ProfileOverview>>("/api/profiles"));
    }

    [Fact, Trait("Scenario", "UC9-S3")]
    public async Task S3_CreareUnProfilo()
    {
        var alfa = await GivenTenantAsync(
            "Alfa Srl",
            profiles: new[] { new { key = "op", name = "Operatore" } },
            productiveUnits: new[] { new { name = "Stabilimento", profileKeys = new[] { "op" } } });

        var created = await ReadAsync<ProfileOverview>(
            await CreateAsync(new { tenantId = alfa.Id, name = " Revisore ", description = "Sola lettura" }),
            HttpStatusCode.Created);

        Assert.Equal(("Revisore", "Sola lettura", alfa.Id.ToString(), "Alfa Srl"),
            (created.Name, created.Description, created.TenantId, created.TenantCompanyName));
        var scheda = await TenantAsync(alfa.Id);
        Assert.Equal(created.Id, scheda.Profile("Revisore").Id);
        Assert.Equal([scheda.Profile("Operatore").Id], scheda.Unit("Stabilimento").ProfileIds);
        Assert.Contains(await GetOkAsync<List<ProfileOverview>>("/api/profiles"), profile => profile.Id == created.Id);
    }

    [Fact, Trait("Scenario", "UC9-S4")]
    public async Task S4_ProfiloNonValido()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });

        var duplicate = await ErrorsAsync(await CreateAsync(new { tenantId = alfa.Id, name = "OPERATORE" }));
        var withoutName = await ErrorsAsync(await CreateAsync(new { tenantId = alfa.Id, name = "" }));
        var withoutTenant = await ErrorsAsync(await CreateAsync(new { name = "Revisore" }));
        var missingTenant = await ErrorsAsync(await CreateAsync(new { tenantId = Guid.NewGuid(), name = "Revisore" }));

        Assert.Equal("NotUnique", duplicate["name"]);
        Assert.Equal("Required", withoutName["name"]);
        Assert.Equal("Required", withoutTenant["tenantId"]);
        Assert.Equal("InvalidValue", missingTenant["tenantId"]);
        Assert.Single((await TenantAsync(alfa.Id)).Profiles);
    }

    [Fact, Trait("Scenario", "UC9-S5")]
    public async Task S5_EliminareUnProfilo()
    {
        var alfa = await GivenTenantAsync(
            "Alfa Srl",
            profiles: new[] { new { key = "mag", name = "Magazziniere" }, new { key = "op", name = "Operatore" } },
            productiveUnits: new[] { new { name = "Magazzino", profileKeys = new[] { "mag", "op" } } });
        KeycloakUsers.Add("Mario", "Rossi", "mario.rossi@example.com", alfa.Profile("Magazziniere").Id);

        var response = await Admin.DeleteAsync($"/api/profiles/{alfa.Profile("Magazziniere").Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var scheda = await TenantAsync(alfa.Id);
        Assert.Equal("Operatore", Assert.Single(scheda.Profiles).Name);
        Assert.Equal([scheda.Profile("Operatore").Id], scheda.Unit("Magazzino").ProfileIds);
        var mario = Assert.Single((await GetOkAsync<PageView<UserView>>("/api/users?search=Mario")).Items);
        Assert.Empty(mario.Profiles);
    }

    [Fact, Trait("Scenario", "UC9-S6")]
    public async Task S6_EliminareUnProfiloInesistente()
    {
        var response = await Admin.DeleteAsync($"/api/profiles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<HttpResponseMessage> CreateAsync(object body) => Admin.PostAsJsonAsync("/api/profiles", body);
}
