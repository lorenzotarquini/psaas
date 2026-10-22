using System.Net;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC18-provisioning.md</summary>
public sealed class UC18_Provisioning(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC18-S1")]
    public async Task S1_HookFallito()
    {
        var alfa = await GivenSubscribedWithFailingWarehouseAsync();

        var subscription = await GetOkAsync<SubscriptionView>($"/api/tenants/{alfa}/subscription");

        Assert.Equal(("Failed", "hook provision → timeout"), (subscription.Module("warehouse").Status, subscription.Module("warehouse").LastError));
        Assert.Equal("Active", subscription.Module("core").Status);
    }

    [Fact, Trait("Scenario", "UC18-S2")]
    public async Task S2_Riprova()
    {
        var alfa = await GivenSubscribedWithFailingWarehouseAsync();
        Host.ModuleHooks.Failing.Clear();

        var subscription = await ReadAsync<SubscriptionView>(await Admin.PostAsync($"/api/tenants/{alfa}/modules/warehouse/retry", null), HttpStatusCode.OK);

        Assert.Equal(("Active", 2), (subscription.Module("warehouse").Status, subscription.Module("warehouse").Attempts));
    }

    [Fact, Trait("Scenario", "UC18-S3")]
    public async Task S3_NelloStorico()
    {
        var alfa = await GivenSubscribedWithFailingWarehouseAsync();
        Host.ModuleHooks.Failing.Clear();
        await Admin.PostAsync($"/api/tenants/{alfa}/modules/warehouse/retry", null);

        var history = await GetOkAsync<PageView<AuditEventView>>($"/api/audit?tenantId={alfa}&action=module.");

        Assert.Contains(history.Items, e => e.Action == "module.provisioning.failed");
        Assert.Contains(history.Items, e => e.Action == "module.provisioned" && e.Details!.Value.GetProperty("module").GetString() == "warehouse");
    }

    private async Task<Guid> GivenSubscribedWithFailingWarehouseAsync()
    {
        await GivenModuleAsync("warehouse");
        var pro = await GivenPlanAsync("pro", moduleIds: "warehouse");
        var alfa = await GivenTenantAsync("Alfa Srl");
        Host.ModuleHooks.Failing.Add("warehouse");
        await SubscribeAsync(alfa.Id, pro.Id);
        return alfa.Id;
    }
}
