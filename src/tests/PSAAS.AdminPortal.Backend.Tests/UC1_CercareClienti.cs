using System.Net;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC1-cercare-clienti.md</summary>
public sealed class UC1_CercareClienti(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC1-S1")]
    public async Task S1_ElencoOrdinatoPerRagioneSocialeEPaginato()
    {
        await GivenTenantAsync("Gamma Spa");
        await GivenTenantAsync("Alfa Srl");
        await GivenTenantAsync("Beta Snc");

        var page1 = await TenantsAsync("?page=1&pageSize=2");
        var page2 = await TenantsAsync("?page=2&pageSize=2");

        Assert.Equal(["Alfa Srl", "Beta Snc"], page1.Items.Select(tenant => tenant.CompanyName));
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(["Gamma Spa"], page2.Items.Select(tenant => tenant.CompanyName));
    }

    [Fact, Trait("Scenario", "UC1-S2")]
    public async Task S2_RicercaPerRagioneSocialeIgnoraMaiuscoleEPunteggiatura()
    {
        await GivenTenantAsync("Alfa Srl");
        await GivenTenantAsync("Beta Snc");

        var result = await TenantsAsync("?search=ALFA%20s.r.l.");

        Assert.Equal(["Alfa Srl"], result.Items.Select(tenant => tenant.CompanyName));
    }

    [Fact, Trait("Scenario", "UC1-S3")]
    public async Task S3_RicercaPerPartitaIva()
    {
        await GivenTenantAsync("Alfa Srl", completeVatNumber: "IT11111111111");
        await GivenTenantAsync("Beta Snc", completeVatNumber: "IT22222222222");

        var result = await TenantsAsync("?search=it2222");

        Assert.Equal(["Beta Snc"], result.Items.Select(tenant => tenant.CompanyName));
    }

    [Fact, Trait("Scenario", "UC1-S4")]
    public async Task S4_RicercaPerPec()
    {
        await GivenTenantAsync("Alfa Srl", certifiedEmail: "alfa@pec.it");
        await GivenTenantAsync("Beta Snc", certifiedEmail: "beta@pec.it");

        var result = await TenantsAsync("?search=BETA@PEC");

        Assert.Equal(["Beta Snc"], result.Items.Select(tenant => tenant.CompanyName));
    }

    [Fact, Trait("Scenario", "UC1-S5")]
    public async Task S5_NessunaCorrispondenzaDaElencoVuoto()
    {
        await GivenTenantAsync("Alfa Srl");

        var result = await TenantsAsync("?search=zeta");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Theory, Trait("Scenario", "UC1-S6")]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task S6_PaginazioneFuoriIntervalloRifiutata(int page, int pageSize)
    {
        var response = await Admin.GetAsync($"/api/tenants?page={page}&pageSize={pageSize}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact, Trait("Scenario", "UC1-S7")]
    public async Task S7_IlRiepilogoMostraLaCitta()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", city: "Roma");

        var summary = Assert.Single((await TenantsAsync()).Items);

        Assert.Equal(alfa.Id, summary.Id);
        Assert.Equal(alfa.VatNumber, summary.VatNumber);
        Assert.Equal(alfa.CompleteVatNumber, summary.CompleteVatNumber);
        Assert.Equal(alfa.CertifiedEmail, summary.CertifiedEmail);
        Assert.Equal("Roma", summary.City);
    }
}
