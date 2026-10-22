using PSAAS.Application.Abstractions.Geography;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Services.Tenants;
using PSAAS.Application.Validators.Tenants;
using PSAAS.Domain;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC3 (Attivare un nuovo cliente): every scenario plus the concurrent-duplicate flow 3b.</summary>
public sealed class TenantCreateServiceTests
{
    private readonly FakeUniquenessChecker _uniqueness = new();
    private readonly FakeAuditLog _audit = new();
    private readonly FakeCityList _cities = new();
    private readonly FakeTenantWriteRepository _repository = new();

    private TenantCreateService Service => new(new TenantCreateValidator(_uniqueness), new TenantAddressValidator(_cities), _repository, _audit, TimeProvider.System);

    private static TenantCreateRequest Valid(
        IReadOnlyList<TenantCreateProfileRequest>? profiles = null,
        IReadOnlyList<TenantCreateProductiveUnitRequest>? units = null,
        string city = "roma",
        string postalCode = "00118") =>
        new(" Alfa Srl ", "it12345678901", " alfa@pec.it ", " Via Roma 1 ", city, postalCode, profiles, units);

    [Fact]
    public async Task UC3_S1_ValidRequest_SavesTheTenantWithDerivedAndOfficialValues()
    {
        var result = await Service.CreateAsync(Valid());

        Assert.True(result.Succeeded);
        var saved = _repository.Saved!;
        Assert.Equal(result.TenantId, saved.Id);
        Assert.Equal(
            ("Alfa Srl", "12345678901", "IT12345678901", "alfa@pec.it"),
            (saved.CompanyName, saved.VatNumber, saved.CompleteVatNumber, saved.CertifiedEmail));
        Assert.Equal(new Address("Via Roma 1", "Roma", "00118", "IT"), saved.Address);
        Assert.Empty(_repository.SavedProfiles);
        Assert.Empty(_repository.SavedProductiveUnits);
    }

    [Fact]
    public async Task UC3_S2_UnitsReferenceTheProfilesOfTheSameRequestByKey()
    {
        await Service.CreateAsync(Valid(
            profiles: [new(" op ", " Operatore ", " Di linea "), new(null, "Ospite", null), new("", "Visitatore", " ")],
            units: [new("Stabilimento", null, [" op "]), new("Magazzino", null)]));

        var profiles = _repository.SavedProfiles.ToArray();
        Assert.Equal(["Operatore", "Ospite", "Visitatore"], profiles.Select(profile => profile.Name));
        Assert.Equal(["Di linea", null, null], profiles.Select(profile => profile.Description));
        Assert.Equal("op", profiles[0].TemporaryId);
        Assert.Equal(3, profiles.Select(profile => profile.TemporaryId).Distinct().Count());
        var units = _repository.SavedProductiveUnits.ToArray();
        Assert.Equal(["op"], units[0].TemporaryProfileIds);
        Assert.Empty(units[1].TemporaryProfileIds);
        Assert.All(units, unit => Assert.Empty(unit.ProfileIds));
    }

    [Fact]
    public async Task UC3_S3_MissingData_ReportsEveryRequiredFieldAndSavesNothing()
    {
        var result = await Service.CreateAsync(new TenantCreateRequest(null, null, null, null, null, null));

        Assert.False(result.Succeeded);
        Assert.Equal(
            ["certifiedEmail", "city", "companyName", "completeVatNumber", "postalCode", "street"],
            result.Errors.Where(error => error.Value == "Required").Select(error => error.Key).Order());
        Assert.Null(_repository.Saved);
    }

    [Fact]
    public async Task UC3_S4_InvalidFormats_AreReportedPerField()
    {
        var result = await Service.CreateAsync(Valid() with
        {
            CompanyName = new string('A', 21), CompleteVatNumber = "12345", CertifiedEmail = "non-una-pec"
        });

        Assert.Equal("MaxLengthExceeded", result.Errors["companyName"]);
        Assert.Equal("InvalidFormat", result.Errors["completeVatNumber"]);
        Assert.Equal("InvalidFormat", result.Errors["certifiedEmail"]);
    }

    [Fact]
    public async Task UC3_S5_ValuesUsedByAnotherTenant_AreNotUnique()
    {
        var other = Guid.NewGuid();
        _uniqueness.Taken["alfasrl"] = other;
        _uniqueness.Taken["IT12345678901"] = other;
        _uniqueness.Taken["ALFA@PEC.IT"] = other;

        var result = await Service.CreateAsync(Valid());

        Assert.Equal(["certifiedEmail", "companyName", "completeVatNumber"], result.Errors.Keys.Order());
        Assert.All(result.Errors.Values, code => Assert.Equal("NotUnique", code));
        Assert.Null(_repository.Saved);
    }

    [Theory]
    [InlineData("Atlantide", "00118", "city")]
    [InlineData("Roma", "20100", "postalCode")]
    public async Task UC3_S6_AddressNotInTheCityList_IsInvalid(string city, string postalCode, string field)
    {
        var result = await Service.CreateAsync(Valid(city: city, postalCode: postalCode));

        Assert.Equal(KeyValuePair.Create(field, "InvalidValue"), Assert.Single(result.Errors));
    }

    [Fact]
    public async Task UC3_S6_TownsWithTheSameName_AreToldApartByPostalCode()
    {
        _cities.Cities.Clear();
        _cities.Cities.AddRange([new ItalianCity("Calliano", "TN", ["38060"]), new ItalianCity("Calliano", "AT", ["14031"])]);

        var result = await Service.CreateAsync(Valid(city: "calliano", postalCode: "14031"));

        Assert.True(result.Succeeded);
        Assert.Equal("Calliano", _repository.Saved!.Address.city);
    }

    [Fact]
    public async Task UC3_S7_CityListUnreachable_AcceptsTheAddressAsTyped()
    {
        _cities.Unavailable = true;

        var result = await Service.CreateAsync(Valid(city: "Atlantide", postalCode: "00000"));

        Assert.True(result.Succeeded);
        Assert.Equal(("Atlantide", "00000"), (_repository.Saved!.Address.city, _repository.Saved.Address.postalCode));
    }

    [Fact]
    public async Task UC3_S8_InvalidStructure_IsReportedByPosition()
    {
        var result = await Service.CreateAsync(Valid(
            profiles: [new("a", "Operatore", null), new("a", "operatore ", null), new("b", " ", null)],
            units: [new("", null, ["inesistente", "a"]), new("Magazzino", null), new("MAGAZZINO", null)]));

        Assert.Equal(
            new Dictionary<string, string>
            {
                ["profiles[1].name"] = "NotUnique",
                ["profiles[1].key"] = "NotUnique",
                ["profiles[2].name"] = "Required",
                ["productiveUnits[0].name"] = "Required",
                ["productiveUnits[0].profileKeys[0]"] = "InvalidValue",
                ["productiveUnits[2].name"] = "NotUnique"
            },
            result.Errors);
        Assert.Null(_repository.Saved);
    }

    [Fact]
    public async Task UC3_3b_ConcurrentDuplicate_IsReportedOnTheTakenField()
    {
        _repository.BeforeSave = () =>
        {
            _uniqueness.Taken["IT12345678901"] = Guid.NewGuid();
            throw new TenantUniquenessConflictException("race");
        };

        var result = await Service.CreateAsync(Valid());

        Assert.Equal(KeyValuePair.Create("completeVatNumber", "NotUnique"), Assert.Single(result.Errors));
    }

    [Fact]
    public async Task UC3_3b_ConflictThatNoFieldExplains_IsNotHidden()
    {
        _repository.BeforeSave = () => throw new TenantUniquenessConflictException("race");

        await Assert.ThrowsAsync<TenantUniquenessConflictException>(() => Service.CreateAsync(Valid()));
    }

    [Fact]
    public async Task UC12_S1_CreationIsAudited()
    {
        var result = await Service.CreateAsync(Valid());

        var record = Assert.Single(_audit.Records);
        Assert.Equal(("tenant.created", result.TenantId.ToString(), "Alfa Srl"), (record.Action, record.TenantId, record.TargetName));
    }
}
