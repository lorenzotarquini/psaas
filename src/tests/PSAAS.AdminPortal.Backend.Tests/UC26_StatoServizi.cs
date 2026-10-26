using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC26-stato-servizi.md</summary>
public sealed class UC26_StatoServizi(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC26-S1")]
    public async Task S1_TuttiOperativi()
    {
        var health = await GetOkAsync<ServicesHealthView>("/api/system/services");

        foreach (var key in new[] { "database", "keycloak", "keycloak-admin", "city-list", "module-hooks" })
        {
            Assert.Equal("Up", health.Service(key).Status);
        }

        Assert.Equal("NotConfigured", health.Service("invoice-transmission").Status);
        Assert.Equal("Up", health.Status);
    }

    [Fact, Trait("Scenario", "UC26-S2")]
    public async Task S2_KeycloakGiu()
    {
        KeycloakUsers.Unavailable = true;

        var health = await GetOkAsync<ServicesHealthView>("/api/system/services");

        Assert.Equal(("Down", "Down", "Up"), (health.Service("keycloak-admin").Status, health.Status, health.Service("database").Status));
    }

    [Fact, Trait("Scenario", "UC26-S3")]
    public async Task S3_ModuliInErrore()
    {
        await GivenModuleAsync("warehouse");
        var alfa = await GivenTenantAsync("Alfa Srl");
        Host.ModuleHooks.Failing.Add("warehouse");
        await SubscribeAsync(alfa.Id, (await GivenPlanAsync("pro", moduleIds: "warehouse")).Id);

        var hooks = (await GetOkAsync<ServicesHealthView>("/api/system/services")).Service("module-hooks");

        Assert.Equal(("Degraded", "1"), (hooks.Status, hooks.Detail));
    }
}
