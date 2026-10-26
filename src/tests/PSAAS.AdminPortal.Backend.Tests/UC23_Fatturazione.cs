using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC23-fatturazione.md</summary>
public sealed class UC23_Fatturazione(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC23-S1")]
    public async Task S1_PrimaFatturaAllaSottoscrizione()
    {
        var (alfa, invoice) = await GivenInvoicedTenantAsync();

        Assert.Equal((100m, 22m, 122m, "Issued"), (invoice.Net, invoice.Vat, invoice.Total, invoice.Status));
        Assert.Equal($"{Host.Clock.GetUtcNow().Year}/0001", invoice.Number);
        Assert.InRange((invoice.DueAt - invoice.IssuedAt).TotalDays, 29.99, 30.01);
        Assert.Single((await GetOkAsync<PageView<AuditEventView>>($"/api/audit?tenantId={alfa.Id}&action=invoice.issued")).Items);
    }

    [Fact, Trait("Scenario", "UC23-S2")]
    public async Task S2_RinnovoMensile()
    {
        var (alfa, _) = await GivenInvoicedTenantAsync();
        var renewsAt = (await GetOkAsync<SubscriptionView>($"/api/tenants/{alfa.Id}/subscription")).RenewsAt;

        await DaysPassAsync(31);
        await DaysPassAsync(0);

        Assert.Equal(2, (await InvoicesOfAsync(alfa.Id)).Count);
        Assert.Equal(renewsAt.AddDays(30), (await GetOkAsync<SubscriptionView>($"/api/tenants/{alfa.Id}/subscription")).RenewsAt);
    }

    [Fact, Trait("Scenario", "UC23-S3")]
    public async Task S3_FineProva()
    {
        var pro = await GivenPlanAsync("pro", trialDays: 14);
        var body = TenantBody("Alfa Srl");
        body["draft"] = true;
        var alfa = await ReadAsync<TenantDetail>(await ActivateAsync(body), HttpStatusCode.Created);
        await SubscribeAsync(alfa.Id, pro.Id, trial: true);
        await Admin.PostAsJsonAsync($"/api/tenants/{alfa.Id}/status", new { status = "Active" });

        await DaysPassAsync(15);

        Assert.Equal("Active", (await GetOkAsync<SubscriptionView>($"/api/tenants/{alfa.Id}/subscription")).Status);
        Assert.Equal("Active", (await TenantAsync(alfa.Id)).Lifecycle!.Status);
        Assert.Single(await InvoicesOfAsync(alfa.Id));
    }

    [Fact, Trait("Scenario", "UC23-S4")]
    public async Task S4_CambioPianoProRata()
    {
        var basePlan = await GivenPlanAsync("base", 100);
        var pro = await GivenPlanAsync("pro", 160);
        var alfa = await GivenTenantAsync("Alfa Srl");
        await SubscribeAsync(alfa.Id, basePlan.Id);
        Host.Clock.Advance(TimeSpan.FromDays(15));

        await SubscribeAsync(alfa.Id, pro.Id);
        await SubscribeAsync(alfa.Id, basePlan.Id);

        var invoices = await InvoicesOfAsync(alfa.Id);
        Assert.Contains(invoices, i => i.Kind == "Invoice" && i.Net == 30m);
        Assert.Contains(invoices, i => i.Kind == "CreditNote" && i.Net == 30m);
    }

    [Fact, Trait("Scenario", "UC23-S5")]
    public async Task S5_SenzaRinnovoAutomatico()
    {
        var pro = await GivenPlanAsync("pro");
        var alfa = await GivenTenantAsync("Alfa Srl");
        await Admin.PutAsJsonAsync($"/api/tenants/{alfa.Id}/subscription", new { planId = pro.Id, trial = false, autoRenew = false });

        await DaysPassAsync(31);

        Assert.Equal("Expired", (await GetOkAsync<SubscriptionView>($"/api/tenants/{alfa.Id}/subscription")).Status);
        Assert.Single(await InvoicesOfAsync(alfa.Id));
    }
}
