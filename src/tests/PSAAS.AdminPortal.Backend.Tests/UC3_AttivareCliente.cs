using System.Net;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC3-attivare-cliente.md</summary>
public sealed class UC3_AttivareCliente(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC3-S1")]
    public async Task S1_AttivazioneConSolaAnagrafica()
    {
        var response = await ActivateAsync(TenantBody(
            "Alfa Srl", "it12345678901", "alfa@pec.it", "Via Roma 1", "roma", "00118"));

        var created = await ReadAsync<TenantDetail>(response, HttpStatusCode.Created);
        Assert.Equal($"/api/tenants/{created.Id}", response.Headers.Location!.AbsolutePath);
        var scheda = await TenantAsync(created.Id);
        Assert.Equal("12345678901", scheda.VatNumber);
        Assert.Equal("IT12345678901", scheda.CompleteVatNumber);
        Assert.Equal(new AddressView("Via Roma 1", "Roma", "00118", "IT"), scheda.Address);
        Assert.Equal([created.Id], (await TenantsAsync()).Items.Select(tenant => tenant.Id));
    }

    [Fact, Trait("Scenario", "UC3-S2")]
    public async Task S2_AttivazioneConProfiliEUnitaCollegati()
    {
        var body = TenantBody("Alfa Srl");
        body["profiles"] = new[] { new { key = "op", name = "Operatore" }, new { key = "osp", name = "Ospite" } };
        body["productiveUnits"] = new object[]
        {
            new { name = "Stabilimento", profileKeys = new[] { "op" } },
            new { name = "Magazzino" }
        };

        var created = await ReadAsync<TenantDetail>(await ActivateAsync(body), HttpStatusCode.Created);

        var scheda = await TenantAsync(created.Id);
        Assert.Equal([scheda.Profile("Operatore").Id], scheda.Unit("Stabilimento").ProfileIds);
        Assert.Empty(scheda.Unit("Magazzino").ProfileIds);
        Assert.DoesNotContain(scheda.ProductiveUnits, unit => unit.ProfileIds.Contains(scheda.Profile("Ospite").Id));
    }

    [Fact, Trait("Scenario", "UC3-S3")]
    public async Task S3_DatiObbligatoriMancanti()
    {
        var errors = await ErrorsAsync(await ActivateAsync(new { }));

        foreach (var field in new[] { "companyName", "completeVatNumber", "certifiedEmail", "street", "city", "postalCode" })
        {
            Assert.Equal("Required", errors[field]);
        }

        Assert.Empty((await TenantsAsync()).Items);
    }

    [Fact, Trait("Scenario", "UC3-S4")]
    public async Task S4_FormatiNonValidi()
    {
        var errors = await ErrorsAsync(await ActivateAsync(TenantBody(new string('A', 21), "12345", "non-una-pec")));

        Assert.Equal("MaxLengthExceeded", errors["companyName"]);
        Assert.Equal("InvalidFormat", errors["completeVatNumber"]);
        Assert.Equal("InvalidFormat", errors["certifiedEmail"]);
    }

    [Fact, Trait("Scenario", "UC3-S5")]
    public async Task S5_DatiUniciGiaUsati()
    {
        await GivenTenantAsync("Alfa Srl", "IT11111111111", "alfa@pec.it");

        var errors = await ErrorsAsync(await ActivateAsync(TenantBody("ALFA s.r.l.", "it11111111111", "ALFA@PEC.IT")));

        Assert.Equal("NotUnique", errors["companyName"]);
        Assert.Equal("NotUnique", errors["completeVatNumber"]);
        Assert.Equal("NotUnique", errors["certifiedEmail"]);
        Assert.Equal(1, (await TenantsAsync()).TotalCount);
    }

    [Theory, Trait("Scenario", "UC3-S6")]
    [InlineData("Atlantide", "00118", "city")]
    [InlineData("Roma", "20100", "postalCode")]
    public async Task S6_IndirizzoNonPresenteNellaListaComuni(string city, string postalCode, string field)
    {
        var errors = await ErrorsAsync(await ActivateAsync(TenantBody("Alfa Srl", city: city, postalCode: postalCode)));

        Assert.Equal("InvalidValue", Assert.Single(errors, error => error.Key == field).Value);
    }

    [Fact, Trait("Scenario", "UC3-S7")]
    public async Task S7_ListaComuniNonRaggiungibile()
    {
        CityList.Unavailable = true;

        var created = await ReadAsync<TenantDetail>(
            await ActivateAsync(TenantBody("Alfa Srl", city: "Atlantide", postalCode: "00000")),
            HttpStatusCode.Created);

        Assert.Equal("Atlantide", created.Address.City);
        Assert.Equal("00000", created.Address.PostalCode);
    }

    [Fact, Trait("Scenario", "UC3-S8")]
    public async Task S8_OrganizzazioneNonValida()
    {
        var body = TenantBody("Alfa Srl");
        body["profiles"] = new[] { new { key = "a", name = "Operatore" }, new { key = "a", name = "operatore " } };
        body["productiveUnits"] = new[] { new { name = "", profileKeys = new[] { "inesistente" } } };

        var errors = await ErrorsAsync(await ActivateAsync(body));

        Assert.Equal("NotUnique", errors["profiles[1].name"]);
        Assert.Equal("NotUnique", errors["profiles[1].key"]);
        Assert.Equal("Required", errors["productiveUnits[0].name"]);
        Assert.Equal("InvalidValue", errors["productiveUnits[0].profileKeys[0]"]);
        Assert.Empty((await TenantsAsync()).Items);
    }
}
