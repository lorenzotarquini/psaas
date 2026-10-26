using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC28-notifiche.md</summary>
public sealed class UC28_Notifiche(PostgresFixture database) : AcceptanceTest(database)
{
    private Task<List<NotificationView>> NotificationsAsync() => GetOkAsync<List<NotificationView>>("/api/notifications");

    [Fact, Trait("Scenario", "UC28-S1")]
    public async Task S1_CodaOrdinata()
    {
        var draft = TenantBody("Gamma Spa");
        draft["draft"] = true;
        await ActivateAsync(draft);
        var (alfa, _) = await GivenInvoicedTenantAsync("Alfa Srl");
        await DaysPassAsync(31);
        var beta = await GivenTenantAsync("Beta Snc");
        await SubscribeAsync(beta.Id, (await GivenPlanAsync("prova", trialDays: 3)).Id, trial: true);

        var queue = await NotificationsAsync();

        Assert.Equal(
            [("invoice.overdue", "Alfa Srl"), ("trial.ending", "Beta Snc"), ("draft.stalled", "Gamma Spa")],
            queue.Where(n => n.Kind != "invoice.overdue" || n.TenantId == alfa.Id).Select(n => (n.Kind, n.TenantName)).Distinct());
    }

    [Fact, Trait("Scenario", "UC28-S2")]
    public async Task S2_SparisceQuandoRisolto()
    {
        var (_, invoice) = await GivenInvoicedTenantAsync();
        await DaysPassAsync(31);
        Assert.Contains(await NotificationsAsync(), n => n.Kind == "invoice.overdue");

        foreach (var open in (await GetOkAsync<PageView<InvoiceView>>("/api/invoices?status=Overdue")).Items)
        {
            await Admin.PostAsJsonAsync($"/api/invoices/{open.Id}/payments", new { paidAt = Host.Clock.GetUtcNow().ToString("yyyy-MM-dd") });
        }

        Assert.DoesNotContain(await NotificationsAsync(), n => n.Kind == "invoice.overdue");
    }

    [Fact, Trait("Scenario", "UC28-S3")]
    public async Task S3_LimiteUtenti()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        for (var i = 0; i < 9; i++) KeycloakUsers.Add("Utente", $"N{i}", $"u{i}@example.com", alfa.Profile("Operatore").Id);
        await SubscribeAsync(alfa.Id, (await GivenPlanAsync("pro", maxUsers: 10)).Id);

        var item = Assert.Single(await NotificationsAsync());

        Assert.Equal(("users.limit", 9, 10), (item.Kind, item.Value, item.Limit));
    }

    [Fact, Trait("Scenario", "UC28-S4")]
    public async Task S4_ModuloFallito()
    {
        await GivenModuleAsync("warehouse");
        var alfa = await GivenTenantAsync("Alfa Srl");
        Host.ModuleHooks.Failing.Add("warehouse");
        await SubscribeAsync(alfa.Id, (await GivenPlanAsync("pro", price: 0, moduleIds: "warehouse")).Id);

        var item = Assert.Single(await NotificationsAsync());

        Assert.Equal(("module.failed", "critical", "warehouse"), (item.Kind, item.Severity, item.Subject));
    }
}
