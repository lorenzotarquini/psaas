using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC4-modificare-anagrafica.md</summary>
public sealed class UC4_ModificareAnagrafica(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC4-S1")]
    public async Task S1_ModificaDiTuttiIDatiPartitaIvaCompresa()
    {
        var alfa = await GivenTenantAsync(
            "Alfa Srl",
            completeVatNumber: "IT11111111111",
            profiles: new[] { new { key = "op", name = "Operatore" } });

        var response = await EditAsync(alfa.Id, TenantBody(
            "Alfa Spa", "IT99999999999", "nuova@pec.it", "Corso Buenos Aires 10", "Milano", "20124"));

        await ReadAsync<TenantDetail>(response, HttpStatusCode.OK);
        var scheda = await TenantAsync(alfa.Id);
        Assert.Equal("Alfa Spa", scheda.CompanyName);
        Assert.Equal("99999999999", scheda.VatNumber);
        Assert.Equal("IT99999999999", scheda.CompleteVatNumber);
        Assert.Equal("nuova@pec.it", scheda.CertifiedEmail);
        Assert.Equal(new AddressView("Corso Buenos Aires 10", "Milano", "20124", "IT"), scheda.Address);
        Assert.Equal(alfa.Profiles, scheda.Profiles);
    }

    [Fact, Trait("Scenario", "UC4-S2")]
    public async Task S2_SalvareValoriInvariatiNonEUnDuplicato()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");

        var response = await EditAsync(alfa.Id, TenantBody(
            alfa.CompanyName, alfa.CompleteVatNumber, alfa.CertifiedEmail,
            alfa.Address.Street, alfa.Address.City, alfa.Address.PostalCode));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact, Trait("Scenario", "UC4-S3")]
    public async Task S3_DatiUniciDiUnAltroCliente()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");
        var beta = await GivenTenantAsync("Beta Snc");

        var errors = await ErrorsAsync(await EditAsync(beta.Id, TenantBody(
            alfa.CompanyName, alfa.CompleteVatNumber, alfa.CertifiedEmail)));

        Assert.Equal("NotUnique", errors["companyName"]);
        Assert.Equal("NotUnique", errors["completeVatNumber"]);
        Assert.Equal("NotUnique", errors["certifiedEmail"]);
        Assert.Equal(beta, await TenantAsync(beta.Id), TenantComparer);
    }

    [Fact, Trait("Scenario", "UC4-S4")]
    public async Task S4_DatiNonValidi()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");

        var errors = await ErrorsAsync(await EditAsync(alfa.Id, TenantBody("", "12345", alfa.CertifiedEmail)));

        Assert.Equal("Required", errors["companyName"]);
        Assert.Equal("InvalidFormat", errors["completeVatNumber"]);
    }

    [Fact, Trait("Scenario", "UC4-S5")]
    public async Task S5_IndirizzoNonPresenteNellaListaComuni()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");

        var errors = await ErrorsAsync(await EditAsync(alfa.Id, TenantBody(
            alfa.CompanyName, alfa.CompleteVatNumber, alfa.CertifiedEmail, city: "Roma", postalCode: "20100")));

        Assert.Equal("InvalidValue", errors["postalCode"]);
    }

    [Fact, Trait("Scenario", "UC4-S6")]
    public async Task S6_ClienteInesistente()
    {
        var response = await EditAsync(Guid.NewGuid(), TenantBody("Alfa Srl"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<HttpResponseMessage> EditAsync(Guid id, object body) => Admin.PutAsJsonAsync($"/api/tenants/{id}", body);

    // Records holding lists compare by reference: compare the visible data instead.
    private static readonly IEqualityComparer<TenantDetail> TenantComparer = EqualityComparer<TenantDetail>.Create(
        (a, b) => a!.Id == b!.Id && a.CompanyName == b.CompanyName && a.CompleteVatNumber == b.CompleteVatNumber
            && a.CertifiedEmail == b.CertifiedEmail && a.Address == b.Address);
}
