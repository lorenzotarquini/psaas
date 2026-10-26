using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC29-panoramica.md</summary>
public sealed class UC29_Panoramica(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC29-S1")]
    public async Task S1_Indicatori()
    {
        await GivenTenantAsync("Alfa Srl");
        await GivenTenantAsync("Beta Snc");
        var draft = TenantBody("Gamma Spa");
        draft["draft"] = true;
        var gamma = await ReadAsync<TenantDetail>(await ActivateAsync(draft), HttpStatusCode.Created);
        await SubscribeAsync(gamma.Id, (await GivenPlanAsync("prova", trialDays: 3)).Id, trial: true);
        await Admin.PostAsJsonAsync($"/api/tenants/{gamma.Id}/status", new { status = "Active" });
        foreach (var name in new[] { "mario", "anna", "luca" }) KeycloakUsers.Add(name, "Rossi", $"{name}@example.com");
        await GivenModuleAsync("warehouse");
        await Admin.PostAsJsonAsync("/api/modules", new { id = "hr", name = "Risorse umane", version = "0.9.2", status = "Beta" });

        var kpis = await GetOkAsync<OverviewView>("/api/overview");

        Assert.Equal((2, 2, 1, 1, 3, 3, 1), (kpis.ActiveTenants, kpis.ActiveTenantsThisMonth, kpis.TrialTenants, kpis.TrialsEndingSoon, kpis.Users, kpis.Modules, kpis.ModulesInBeta));
    }

    [Fact, Trait("Scenario", "UC29-S2")]
    public async Task S2_InvitiNonCompletati()
    {
        var now = Host.Clock.GetUtcNow();
        var giulia = KeycloakUsers.AddInvited("Giulia", "Ferri", "giulia@example.com", now.AddDays(-6));
        var mario = KeycloakUsers.AddInvited("Mario", "Rossi", "mario@example.com", now.AddDays(-6));
        KeycloakUsers.AddInvited("Anna", "Bianchi", "anna@example.com", now.AddDays(-1));

        var item = (await GetOkAsync<List<NotificationView>>("/api/notifications")).Single(n => n.Kind == "users.invites");
        var resend = await Admin.PostAsync("/api/users/invites/resend", null);

        Assert.Equal(2, item.Value);
        Assert.Equal(HttpStatusCode.OK, resend.StatusCode);
        Assert.Equal([(giulia.Id, "invite"), (mario.Id, "invite")], KeycloakUsers.ActionEmails.Order());
    }
}
