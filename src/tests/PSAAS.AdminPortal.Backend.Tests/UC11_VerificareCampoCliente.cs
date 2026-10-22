using System.Net;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC11-verificare-campo-cliente.md</summary>
public sealed class UC11_VerificareCampoCliente(PostgresFixture database) : AcceptanceTest(database)
{
    [Theory, Trait("Scenario", "UC11-S1")]
    [InlineData("companyName", "Beta Snc")]
    [InlineData("completeVatNumber", "IT22222222222")]
    [InlineData("certifiedEmail", "beta@pec.it")]
    public async Task S1_ValoreLibero(string field, string value)
    {
        await GivenTenantAsync("Alfa Srl", "IT11111111111", "alfa@pec.it");

        Assert.Null((await ValidateAsync(field, value)).ErrorCode);
    }

    [Theory, Trait("Scenario", "UC11-S2")]
    [InlineData("companyName", "ALFA s.r.l.")]
    [InlineData("completeVatNumber", "it11111111111")]
    [InlineData("certifiedEmail", "ALFA@PEC.IT")]
    public async Task S2_ValoreGiaUsato(string field, string value)
    {
        await GivenTenantAsync("Alfa Srl", "IT11111111111", "alfa@pec.it");

        Assert.Equal("NotUnique", (await ValidateAsync(field, value)).ErrorCode);
    }

    [Fact, Trait("Scenario", "UC11-S3")]
    public async Task S3_IValoriDelClienteInModificaNonSonoDuplicati()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");

        Assert.Null((await ValidateAsync("companyName", "Alfa Srl", alfa.Id)).ErrorCode);
    }

    [Theory, Trait("Scenario", "UC11-S4")]
    [InlineData("companyName", "", "Required")]
    [InlineData("companyName", "AAAAAAAAAAAAAAAAAAAAA", "MaxLengthExceeded")]
    [InlineData("completeVatNumber", "12345", "InvalidFormat")]
    [InlineData("certifiedEmail", "non-una-pec", "InvalidFormat")]
    public async Task S4_StesseRegoleDelSalvataggio(string field, string value, string expected)
    {
        Assert.Equal(expected, (await ValidateAsync(field, value)).ErrorCode);
    }

    [Fact, Trait("Scenario", "UC11-S5")]
    public async Task S5_CampoNonVerificabile()
    {
        var errors = await ErrorsAsync(await Admin.GetAsync("/api/tenants/field-validation?field=street&value=Via%20Roma"));

        Assert.Equal("InvalidValue", errors["field"]);
    }

    [Fact, Trait("Scenario", "UC11-S6")]
    public async Task S6_IndicaIlClienteCheUsaGiaIlValore()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");

        var result = await ValidateAsync("companyName", "ALFA s.r.l.");

        Assert.Equal("NotUnique", result.ErrorCode);
        Assert.Equal(new TenantReferenceView(alfa.Id, "Alfa Srl"), result.ConflictingTenant);
    }

    private async Task<FieldValidationView> ValidateAsync(string field, string value, Guid? excludeTenantId = null)
    {
        var url = $"/api/tenants/field-validation?field={field}&value={Uri.EscapeDataString(value)}";
        if (excludeTenantId is { } id) url += $"&excludeTenantId={id}";
        return await ReadAsync<FieldValidationView>(await Admin.GetAsync(url), HttpStatusCode.OK);
    }
}
