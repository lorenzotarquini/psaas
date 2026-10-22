using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Services.Tenants;
using PSAAS.Application.Validators.Tenants;
using PSAAS.Domain;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC4 (Modificare l'anagrafica di un cliente).</summary>
public sealed class TenantRegistryUpdateServiceTests
{
    private static readonly Tenant Alfa = Tenant.Create(
        "Alfa Srl", "11111111111", "IT11111111111", "alfa@pec.it", new Address("Via Roma 1", "Roma", "00118", "IT"));

    private readonly FakeTenantReadRepository _reader = new() { Tenant = Alfa };
    private readonly FakeAuditLog _audit = new();
    private readonly FakeUniquenessChecker _uniqueness = new();
    private readonly FakeTenantWriteRepository _writer = new();

    private TenantRegistryUpdateService Service => new(
        _reader, new TenantCreateValidator(_uniqueness), new TenantAddressValidator(new FakeCityList()), _writer, _audit);

    private static TenantRegistryUpdateRequest Request(string companyName = "Alfa Spa", string vat = "it99999999999") =>
        new(companyName, vat, " nuova@pec.it ", "Via Nuova 2", "roma", "00118");

    [Fact]
    public async Task UC4_S1_SavesEveryFieldVatIncludedWithTheSameIdentity()
    {
        var result = await Service.UpdateAsync(Alfa.Id, Request());

        Assert.True(result.Succeeded);
        var saved = _writer.Saved!;
        Assert.Equal(
            (Alfa.Id, "Alfa Spa", "99999999999", "IT99999999999", "nuova@pec.it"),
            (saved.Id, saved.CompanyName, saved.VatNumber, saved.CompleteVatNumber, saved.CertifiedEmail));
        Assert.Equal(new Address("Via Nuova 2", "Roma", "00118", "IT", "RM"), saved.Address);
    }

    [Fact]
    public async Task UC4_S2_OwnUnchangedValuesAreNotDuplicates()
    {
        _uniqueness.Taken["alfasrl"] = Alfa.Id;
        _uniqueness.Taken["IT11111111111"] = Alfa.Id;

        var result = await Service.UpdateAsync(Alfa.Id, Request("Alfa Srl", "IT11111111111"));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task UC4_S3_ValuesOfAnotherTenantAreNotUnique()
    {
        _uniqueness.Taken["IT99999999999"] = Guid.NewGuid();

        var result = await Service.UpdateAsync(Alfa.Id, Request());

        Assert.Equal("NotUnique", result.Errors["completeVatNumber"]);
        Assert.Null(_writer.Saved);
    }

    [Fact]
    public async Task UC4_S6_MissingTenant_IsNotFound()
    {
        var result = await Service.UpdateAsync(Guid.NewGuid(), Request());

        Assert.False(result.Found);
    }

    [Fact]
    public async Task UC4_ConcurrentDuplicate_ReportsTheField()
    {
        _writer.BeforeSave = () =>
        {
            _uniqueness.Taken["NUOVA@PEC.IT"] = Guid.NewGuid();
            throw new TenantUniquenessConflictException("race");
        };

        var result = await Service.UpdateAsync(Alfa.Id, Request());

        Assert.Equal("NotUnique", result.Errors["certifiedEmail"]);
    }

    [Fact]
    public async Task UC12_S2_OnlyTheChangedFieldsAreAudited_WithBeforeAndAfter()
    {
        await Service.UpdateAsync(Alfa.Id, Request("Alfa Srl", "IT11111111111"));

        var record = Assert.Single(_audit.Records);
        Assert.Equal("tenant.registry.updated", record.Action);
        var changes = Assert.IsType<RegistryChanges>(record.Details).Changes;
        Assert.Equal(["certifiedEmail", "street"], changes.Keys.Order());
        Assert.Equal(new("alfa@pec.it", "nuova@pec.it"), changes["certifiedEmail"]);
    }

    [Fact]
    public async Task UC12_S2_SavingWithoutChanges_IsNotAudited()
    {
        var unchanged = new TenantRegistryUpdateRequest("Alfa Srl", "IT11111111111", "alfa@pec.it", "Via Roma 1", "Roma", "00118");

        await Service.UpdateAsync(Alfa.Id, unchanged);

        Assert.Empty(_audit.Records);
    }
}
