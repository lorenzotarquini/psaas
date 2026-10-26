using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC25-uso-cliente.md</summary>
public sealed class UC25_UsoCliente(PostgresFixture database) : AcceptanceTest(database)
{
    private async Task<TenantDetail> GivenTenantWithWarehouseAsync(int? maxUsers = null, object? profiles = null, object? units = null)
    {
        await GivenModuleAsync("warehouse");
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: profiles, productiveUnits: units);
        var plan = await ReadAsync<PlanView>(await Admin.PostAsJsonAsync("/api/plans", new
        {
            code = "pro", name = "Pro", price = 100, billingPeriod = "Monthly", maxUsers, maxProductiveUnits = 3, moduleIds = new[] { "warehouse" },
        }), HttpStatusCode.Created);
        await SubscribeAsync(alfa.Id, plan.Id);
        return alfa;
    }

    private static object Row(Guid tenantId, string moduleId, string metric, decimal value, string kind = "counter") =>
        new { tenantId, moduleId, metric, value, kind };

    private Task<HttpResponseMessage> ReportAsync(HttpClient client, params object[] records) =>
        client.PostAsJsonAsync("/api/usage", new { records });

    private HttpClient Module() => ClientWith(KeycloakTokens.Create(["UsageReporter"], subject: "svc-warehouse", userName: "service-account-warehouse"));

    [Fact, Trait("Scenario", "UC25-S1")]
    public async Task S1_ContatoriEMisure()
    {
        var alfa = await GivenTenantWithWarehouseAsync();

        await ReportAsync(Module(), Row(alfa.Id, "warehouse", "documents.created", 3), Row(alfa.Id, "warehouse", "storage.mb", 120, "gauge"));
        await ReportAsync(Module(), Row(alfa.Id, "warehouse", "documents.created", 2), Row(alfa.Id, "warehouse", "storage.mb", 150, "gauge"));

        var usage = await GetOkAsync<TenantUsageView>($"/api/tenants/{alfa.Id}/usage");
        Assert.Equal(5m, usage.Metrics.Single(m => m.Metric == "documents.created").Total);
        Assert.Equal(150m, usage.Metrics.Single(m => m.Metric == "storage.mb").Total);
    }

    [Fact, Trait("Scenario", "UC25-S2")]
    public async Task S2_LimitiDelPiano()
    {
        var alfa = await GivenTenantWithWarehouseAsync(
            maxUsers: 10,
            profiles: new[] { new { key = "op", name = "Operatore" } },
            units: new[] { new { name = "Stabilimento", profileKeys = new[] { "op" } } });
        var mario = KeycloakUsers.Add("Mario", "Rossi", "mario@example.com", alfa.Profile("Operatore").Id);
        KeycloakUsers.Add("Anna", "Bianchi", "anna@example.com", alfa.Profile("Operatore").Id);
        KeycloakUsers.LastLogins[mario.Id] = Host.Clock.GetUtcNow().AddDays(-1);

        var usage = await GetOkAsync<TenantUsageView>($"/api/tenants/{alfa.Id}/usage");

        Assert.Equal((2, 10, 1, 1, 3), (usage.Users, usage.MaxUsers, usage.ActiveUsers, usage.ProductiveUnits, usage.MaxProductiveUnits));
    }

    [Fact, Trait("Scenario", "UC25-S3")]
    public async Task S3_DatiNonValidi()
    {
        var alfa = await GivenTenantWithWarehouseAsync();

        var errors = await ErrorsAsync(await ReportAsync(Module(), Row(alfa.Id, "hr", "documents.created", 1), Row(alfa.Id, "warehouse", "documents.created", -1)));

        Assert.Equal(("InvalidValue", "InvalidValue"), (errors["records[0].moduleId"], errors["records[1].value"]));
        Assert.Empty((await GetOkAsync<TenantUsageView>($"/api/tenants/{alfa.Id}/usage")).Metrics);
    }

    [Fact, Trait("Scenario", "UC25-S4")]
    public async Task S4_RuoloDelModulo()
    {
        var alfa = await GivenTenantWithWarehouseAsync();

        var report = await ReportAsync(Module(), Row(alfa.Id, "warehouse", "documents.created", 1));
        var list = await Module().GetAsync("/api/tenants");

        Assert.Equal((HttpStatusCode.NoContent, HttpStatusCode.Forbidden), (report.StatusCode, list.StatusCode));
    }
}
