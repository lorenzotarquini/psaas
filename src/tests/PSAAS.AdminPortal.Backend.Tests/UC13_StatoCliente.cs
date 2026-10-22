using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC13-stato-cliente.md</summary>
public sealed class UC13_StatoCliente(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC13-S1")]
    public async Task S1_SospensioneConMotivo()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");

        var scheda = await ReadAsync<TenantDetail>(await ChangeStatusAsync(alfa.Id, "Suspended", "Morosità"), HttpStatusCode.OK);

        Assert.Equal(("Suspended", "Morosità"), (scheda.Lifecycle!.Status, scheda.Lifecycle.Reason));
        var e = (await GetOkAsync<PageView<AuditEventView>>($"/api/audit?tenantId={alfa.Id}&action=tenant.status")).Items.Single();
        Assert.Equal("Active", e.Details!.Value.GetProperty("from").GetString());
        Assert.Equal("Suspended", e.Details.Value.GetProperty("to").GetString());
    }

    [Fact, Trait("Scenario", "UC13-S2")]
    public async Task S2_Riattivazione()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");
        await ChangeStatusAsync(alfa.Id, "Suspended", "Morosità");

        var scheda = await ReadAsync<TenantDetail>(await ChangeStatusAsync(alfa.Id, "Active"), HttpStatusCode.OK);

        Assert.Equal("Active", scheda.Lifecycle!.Status);
    }

    [Fact, Trait("Scenario", "UC13-S3")]
    public async Task S3_TransizioneNonConsentita()
    {
        var alfa = await GivenCeasedTenantAsync("Alfa Srl");

        var errors = await ErrorsAsync(await ChangeStatusAsync(alfa.Id, "Active"));

        Assert.Equal("InvalidValue", errors["status"]);
    }

    [Fact, Trait("Scenario", "UC13-S4")]
    public async Task S4_MotivoObbligatorio()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");

        var errors = await ErrorsAsync(await ChangeStatusAsync(alfa.Id, "Suspended", " "));

        Assert.Equal("Required", errors["reason"]);
        Assert.Equal("Active", (await TenantAsync(alfa.Id)).Lifecycle!.Status);
    }

    [Fact, Trait("Scenario", "UC13-S5")]
    public async Task S5_CessatoOccupaIDatiUnivoci()
    {
        await GivenCeasedTenantAsync("Alfa Srl");

        var errors = await ErrorsAsync(await ActivateAsync(TenantBody("Alfa Srl")));

        Assert.Equal("NotUnique", errors["companyName"]);
    }

    [Fact, Trait("Scenario", "UC13-S6")]
    public async Task S6_ClienteInesistente()
    {
        var response = await ChangeStatusAsync(Guid.NewGuid(), "Suspended", "x");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<TenantDetail> GivenCeasedTenantAsync(string companyName)
    {
        var tenant = await GivenTenantAsync(companyName);
        await ChangeStatusAsync(tenant.Id, "Suspended", "Morosità");
        await ChangeStatusAsync(tenant.Id, "Ceased", "Contratto risolto");
        return tenant;
    }

    private Task<HttpResponseMessage> ChangeStatusAsync(Guid id, string status, string? reason = null) =>
        Admin.PostAsJsonAsync($"/api/tenants/{id}/status", new { status, reason });
}
