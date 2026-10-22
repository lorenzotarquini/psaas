using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC21-elenco-clienti.md</summary>
public sealed class UC21_ElencoClienti(PostgresFixture database) : AcceptanceTest(database)
{
    private static List<string> Names(PageView<TenantSummary> page) => page.Items.Select(t => t.CompanyName).ToList();

    [Fact, Trait("Scenario", "UC21-S1")]
    public async Task S1_ConteggiPerScheda()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");
        await GivenTenantAsync("Beta Snc");
        var draft = TenantBody("Bozza Srl");
        draft["draft"] = true;
        await ActivateAsync(draft);
        var gamma = await GivenTenantAsync("Gamma Spa");
        await Admin.PostAsJsonAsync($"/api/tenants/{gamma.Id}/status", new { status = "Suspended", reason = "Morosità" });
        await SubscribeAsync(alfa.Id, (await GivenPlanAsync("base")).Id);

        var counts = await GetOkAsync<TenantCountsView>("/api/tenants/counts");

        Assert.Equal((4, 2, 1, 1, 1), (counts.All, counts.ByStatus["Active"], counts.ByStatus["Onboarding"], counts.ByStatus["Suspended"], counts.RenewingSoon));
    }

    [Fact, Trait("Scenario", "UC21-S2")]
    public async Task S2_FiltroPerPianoEStato()
    {
        var pro = await GivenPlanAsync("pro");
        var basePlan = await GivenPlanAsync("base");
        await SubscribeAsync((await GivenTenantAsync("Alfa Srl")).Id, pro.Id);
        await SubscribeAsync((await GivenTenantAsync("Beta Snc")).Id, basePlan.Id);
        await GivenTenantAsync("Gamma Spa");

        var page = await TenantsAsync($"?planId={pro.Id}&planId={basePlan.Id}&status=Active");

        Assert.Equal(["Alfa Srl", "Beta Snc"], Names(page));
        Assert.Equal(["PRO", "BASE"], page.Items.Select(t => t.PlanName));
    }

    [Fact, Trait("Scenario", "UC21-S3")]
    public async Task S3_FiltroPerModuloEProvincia()
    {
        await GivenModuleAsync("warehouse");
        await SubscribeAsync((await GivenTenantAsync("Alfa Srl")).Id, (await GivenPlanAsync("pro", moduleIds: "warehouse")).Id);
        await GivenTenantAsync("Beta Snc", city: "Milano", postalCode: "20124");

        var byModule = await TenantsAsync("?moduleId=warehouse");
        var byProvince = await TenantsAsync("?province=mi");

        Assert.Equal(["Alfa Srl"], Names(byModule));
        Assert.Equal(("Beta Snc", "MI"), (byProvince.Items.Single().CompanyName, byProvince.Items.Single().Province));
    }

    [Fact, Trait("Scenario", "UC21-S4")]
    public async Task S4_OrdinamentoERinnovo()
    {
        var monthly = await GivenPlanAsync("mensile");
        var yearly = await ReadAsync<PlanView>(
            await Admin.PostAsJsonAsync("/api/plans", new { code = "annuale", name = "Annuale", price = 900, billingPeriod = "Yearly" }), HttpStatusCode.Created);
        await SubscribeAsync((await GivenTenantAsync("Alfa Srl")).Id, monthly.Id);
        await SubscribeAsync((await GivenTenantAsync("Beta Snc")).Id, yearly.Id);

        var sorted = await TenantsAsync("?sort=-renewsAt");
        var soon = await TenantsAsync("?renewalWithinDays=60");

        Assert.Equal(["Beta Snc", "Alfa Srl"], Names(sorted));
        Assert.Equal(["Alfa Srl"], Names(soon));
    }

    [Fact, Trait("Scenario", "UC21-S5")]
    public async Task S5_UtentiRispettoAlLimite()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });
        KeycloakUsers.Add("Mario", "Rossi", "mario@example.com", alfa.Profile("Operatore").Id);
        KeycloakUsers.Add("Anna", "Bianchi", "anna@example.com", alfa.Profile("Operatore").Id);
        await SubscribeAsync(alfa.Id, (await GivenPlanAsync("pro", maxUsers: 50)).Id);

        var row = (await TenantsAsync()).Items.Single();

        Assert.Equal((2, 50), (row.UserCount, row.MaxUsers));
        Assert.Equal(["core"], row.Modules!.Select(m => m.Id));
    }

    [Fact, Trait("Scenario", "UC21-S6")]
    public async Task S6_EsportaCsv()
    {
        await GivenTenantAsync("Alfa Srl");
        var beta = await GivenTenantAsync("Beta Snc");

        var response = await Admin.GetAsync($"/api/tenants/export?ids={beta.Id}");
        var lines = (await response.Content.ReadAsStringAsync()).TrimStart('\uFEFF').TrimEnd().Split('\n');

        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("ragioneSociale,partitaIva,pec", lines[0]);
        Assert.StartsWith("Beta Snc,", lines[1]);
    }

    [Theory, Trait("Scenario", "UC21-S7")]
    [InlineData("?sort=colore", "sort")]
    [InlineData("?renewalWithinDays=0", "renewalWithinDays")]
    [InlineData("?status=Felice", "status")]
    public async Task S7_ParametriNonValidi(string query, string field)
    {
        var errors = await ErrorsAsync(await Admin.GetAsync($"/api/tenants{query}"));

        Assert.Equal("InvalidValue", errors[field]);
    }
}
