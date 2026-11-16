using System.Net;
using System.Net.Http.Json;
using FinHorizon.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC32-moduli-utente.md</summary>
public sealed class UC32_ModuliUtente(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC32-S1")]
    public async Task S1_AssegnareMySync()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com", alfa.Profile("Operatore").Id);

        var modules = await ReadAsync<List<string>>(await Admin.PutAsync($"/api/users/{mario.Id}/modules/mysync", null), HttpStatusCode.OK);

        Assert.Equal(["mysync"], modules);
        Assert.Equal(["mysync"], await GetOkAsync<List<string>>($"/api/users/{mario.Id}/modules"));
        Assert.Equal(["mysync"], KeycloakUsers.Modules[mario.Id]);
        Assert.Single((await GetOkAsync<PageView<AuditEventView>>($"/api/audit?tenantId={alfa.Id}&action=user.module.granted")).Items);
    }

    [Fact, Trait("Scenario", "UC32-S2")]
    public async Task S2_TogliereMySyncChiudeLeSessioni()
    {
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com");
        await Admin.PutAsync($"/api/users/{mario.Id}/modules/mysync", null);

        var modules = await ReadAsync<List<string>>(await Admin.DeleteAsync($"/api/users/{mario.Id}/modules/mysync"), HttpStatusCode.OK);

        Assert.Empty(modules);
        Assert.Empty(await GetOkAsync<List<string>>($"/api/users/{mario.Id}/modules"));
        Assert.Equal([mario.Id], KeycloakUsers.SignedOut);
    }

    [Fact, Trait("Scenario", "UC32-S3")]
    public async Task S3_ModuloSconosciutoODeprecato()
    {
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com");
        await GivenModuleAsync("warehouse");
        await Admin.PutAsJsonAsync("/api/modules/warehouse/status", new { status = "Deprecated" });

        var unknown = await ErrorsAsync(await Admin.PutAsync($"/api/users/{mario.Id}/modules/nope", null));
        var deprecated = await ErrorsAsync(await Admin.PutAsync($"/api/users/{mario.Id}/modules/warehouse", null));

        Assert.Equal("InvalidValue", unknown["moduleId"]);
        Assert.Equal("InvalidValue", deprecated["moduleId"]);
        Assert.False(KeycloakUsers.Modules.ContainsKey(mario.Id));
    }

    [Fact, Trait("Scenario", "UC32-S4")]
    public async Task S4_UtenteInesistente()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.GetAsync("/api/users/kc-nope/modules")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.PutAsync("/api/users/kc-nope/modules/mysync", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.DeleteAsync("/api/users/kc-nope/modules/mysync")).StatusCode);
    }

    [Fact, Trait("Scenario", "UC32-S5")]
    public async Task S5_KeycloakNonRaggiungibile()
    {
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com");
        KeycloakUsers.Unavailable = true;

        var response = await Admin.PutAsync($"/api/users/{mario.Id}/modules/mysync", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
