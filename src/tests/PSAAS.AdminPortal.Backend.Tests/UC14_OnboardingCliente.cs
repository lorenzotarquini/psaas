using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC14-onboarding-cliente.md</summary>
public sealed class UC14_OnboardingCliente(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC14-S1")]
    public async Task S1_BozzaInOnboarding()
    {
        var bozza = await GivenDraftAsync("Alfa Srl", sdiCode: "m5uxcr1");

        Assert.Equal(("Onboarding", "M5UXCR1", "Marta Russo"), (bozza.Lifecycle!.Status, bozza.SdiCode, bozza.AccountManager));
        Assert.StartsWith("tn_", bozza.Code);
        var e = (await GetOkAsync<PageView<AuditEventView>>($"/api/audit?tenantId={bozza.Id}")).Items.Single();
        Assert.Equal("tenant.onboarding.started", e.Action);
    }

    [Fact, Trait("Scenario", "UC14-S2")]
    public async Task S2_AttivazioneDellaBozza()
    {
        var bozza = await GivenDraftAsync("Alfa Srl");

        var scheda = await ReadAsync<TenantDetail>(
            await Admin.PostAsJsonAsync($"/api/tenants/{bozza.Id}/status", new { status = "Active" }), HttpStatusCode.OK);

        Assert.Equal("Active", scheda.Lifecycle!.Status);
    }

    [Fact, Trait("Scenario", "UC14-S3")]
    public async Task S3_AnnullareLaBozza()
    {
        var bozza = await GivenDraftAsync("Alfa Srl", profiles: new[] { new { key = "op", name = "Operatore" } });

        var response = await Admin.DeleteAsync($"/api/tenants/{bozza.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.GetAsync($"/api/tenants/{bozza.Id}")).StatusCode);
        Assert.Empty(await GetOkAsync<List<ProfileOverview>>("/api/profiles"));
        await GivenTenantAsync("Alfa Srl");
    }

    [Fact, Trait("Scenario", "UC14-S4")]
    public async Task S4_ClienteAttivoNonSiElimina()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");

        var response = await Admin.DeleteAsync($"/api/tenants/{alfa.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Alfa Srl", (await TenantAsync(alfa.Id)).CompanyName);
    }

    [Fact, Trait("Scenario", "UC14-S5")]
    public async Task S5_CodiceSdiNonValido()
    {
        var body = TenantBody("Alfa Srl");
        body["sdiCode"] = "ABC";

        var errors = await ErrorsAsync(await ActivateAsync(body));

        Assert.Equal("InvalidFormat", errors["sdiCode"]);
    }

    private async Task<TenantDetail> GivenDraftAsync(string companyName, string? sdiCode = null, object? profiles = null)
    {
        var body = TenantBody(companyName);
        body["draft"] = true;
        body["sdiCode"] = sdiCode;
        body["accountManager"] = "Marta Russo";
        body["profiles"] = profiles;
        return await ReadAsync<TenantDetail>(await ActivateAsync(body), HttpStatusCode.Created);
    }
}
