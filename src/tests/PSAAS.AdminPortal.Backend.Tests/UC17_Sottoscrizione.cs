using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC17-sottoscrizione.md</summary>
public sealed class UC17_Sottoscrizione(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC17-S1")]
    public async Task S1_ModuliDelPianoConDipendenze()
    {
        await GivenModuleAsync("warehouse");
        await GivenModuleAsync("hr", "1.0.0", "warehouse");
        var pro = await GivenPlanAsync("pro", moduleIds: "hr");
        var alfa = await GivenTenantAsync("Alfa Srl");

        var subscription = await ReadAsync<SubscriptionView>(await SubscribeAsync(alfa.Id, pro.Id), HttpStatusCode.OK);

        Assert.Equal(["core", "hr", "warehouse"], subscription.Modules.Select(m => m.ModuleId).Order());
        Assert.All(subscription.Modules, m => Assert.Equal("Active", m.Status));
    }

    [Fact, Trait("Scenario", "UC17-S2")]
    public async Task S2_ProvaGratuita()
    {
        var pro = await GivenPlanAsync("pro", trialDays: 14);
        var body = TenantBody("Alfa Srl");
        body["draft"] = true;
        var alfa = await ReadAsync<TenantDetail>(await ActivateAsync(body), HttpStatusCode.Created);

        var subscription = await ReadAsync<SubscriptionView>(await SubscribeAsync(alfa.Id, pro.Id, trial: true), HttpStatusCode.OK);
        var active = await ReadAsync<TenantDetail>(await Admin.PostAsJsonAsync($"/api/tenants/{alfa.Id}/status", new { status = "Active" }), HttpStatusCode.OK);

        Assert.Equal("Trial", subscription.Status);
        Assert.InRange((subscription.TrialEndsAt!.Value - DateTimeOffset.UtcNow).TotalDays, 13.9, 14.1);
        Assert.Equal("Trial", active.Lifecycle!.Status);
    }

    [Fact, Trait("Scenario", "UC17-S3")]
    public async Task S3_LimiteUtentiDelPiano()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        KeycloakUsers.Add("Mario", "Rossi", "mario@example.com", alfa.Profile("Operatore").Id);
        KeycloakUsers.Add("Anna", "Bianchi", "anna@example.com", alfa.Profile("Operatore").Id);
        var basePlan = await GivenPlanAsync("base", maxUsers: 1);

        var errors = await ErrorsAsync(await SubscribeAsync(alfa.Id, basePlan.Id));

        Assert.Equal("LimitExceeded", errors["planId"]);
    }

    [Fact, Trait("Scenario", "UC17-S4")]
    public async Task S4_PrezzoBloccato()
    {
        var pro = await GivenPlanAsync("pro", 99);
        var alfa = await GivenTenantAsync("Alfa Srl");
        await SubscribeAsync(alfa.Id, pro.Id);

        await Admin.PutAsJsonAsync($"/api/plans/{pro.Id}", new { code = "pro", name = "PRO", price = 129, billingPeriod = "Monthly" });

        Assert.Equal(99m, (await GetOkAsync<SubscriptionView>($"/api/tenants/{alfa.Id}/subscription")).Price);
    }

    [Fact, Trait("Scenario", "UC17-S5")]
    public async Task S5_CambioPianoTogliModuli()
    {
        await GivenModuleAsync("warehouse");
        var pro = await GivenPlanAsync("pro", moduleIds: "warehouse");
        var basePlan = await GivenPlanAsync("base");
        var alfa = await GivenTenantAsync("Alfa Srl");
        await SubscribeAsync(alfa.Id, pro.Id);

        var subscription = await ReadAsync<SubscriptionView>(await SubscribeAsync(alfa.Id, basePlan.Id), HttpStatusCode.OK);

        Assert.Equal(["core"], subscription.Modules.Select(m => m.ModuleId));
    }

    [Fact, Trait("Scenario", "UC17-S6")]
    public async Task S6_NuovoUtenteOltreIlLimite()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        await SubscribeAsync(alfa.Id, (await GivenPlanAsync("base", maxUsers: 1)).Id);
        KeycloakUsers.Add("Mario", "Rossi", "mario@example.com", alfa.Profile("Operatore").Id);

        var errors = await ErrorsAsync(await Admin.PostAsJsonAsync("/api/users", new
        {
            firstName = "Anna", lastName = "Bianchi", email = "anna@example.com", birthDate = "1990-01-01",
            profileIds = new[] { alfa.Profile("Operatore").Id },
        }));

        Assert.Equal("LimitExceeded", errors["profileIds[0]"]);
    }
}
