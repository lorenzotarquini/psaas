using FinHorizon.AdminPortal.Application.Contracts.Tenants;
using FinHorizon.AdminPortal.Application.Validators.Tenants;
using FinHorizon.AdminPortal.Domain;
using Xunit;

namespace FinHorizon.AdminPortal.Application.Tests.Validators;

/// <summary>Derived from UC3-S9, UC3-S10, UC3-S11 and UC11-S7 (rules of a private person).</summary>
public sealed class IndividualRulesTests
{
    private readonly FakeUniquenessChecker _uniqueness = new();

    private TenantCreateValidator Validator => new(_uniqueness);

    [Theory]
    [InlineData("RSSMRA85T10A562S")]
    [InlineData("BNCLRA90A41H501F")]
    [InlineData("VRDGPP80A01F205X")]
    public void TaxCode_WithTheRightCheckLetter_IsValid(string taxCode) => Assert.True(ItalianTaxCode.IsValid(taxCode));

    [Theory]
    [InlineData("RSSMRA85T10A562X")] // wrong check letter
    [InlineData("RSSMRA85Z10A562S")] // no month Z
    [InlineData("RSSMRA85T10A562")] // 15 characters
    [InlineData("IT12345678901")]
    public void TaxCode_Otherwise_IsNotValid(string taxCode) => Assert.False(ItalianTaxCode.IsValid(taxCode));

    [Fact]
    public async Task FullName_IsNeverADuplicateAndMayBeFiftyCharacters()
    {
        _uniqueness.Taken["mariorossi"] = Guid.NewGuid();

        Assert.True((await Validator.ValidateCompanyNameAsync("Mario Rossi", null, default, TenantKind.Individual)).IsValid);
        Assert.True((await Validator.ValidateCompanyNameAsync(new string('a', 50), null, default, TenantKind.Individual)).IsValid);
        Assert.Equal(
            TenantFieldValidationErrorCodes.MaxLengthExceeded,
            (await Validator.ValidateCompanyNameAsync(new string('a', 51), null, default, TenantKind.Individual)).ErrorCode);
    }

    [Fact]
    public async Task TaxCode_IsNormalizedAndUnique()
    {
        _uniqueness.Taken["RSSMRA85T10A562S"] = Guid.NewGuid();

        var result = await Validator.ValidateCompleteVatNumberAsync(" rssmra85t10a562s ", null, default, TenantKind.Individual);

        Assert.Equal(TenantFieldValidationErrorCodes.NotUnique, result.ErrorCode);
        Assert.Equal("RSSMRA85T10A562S", result.NormalizedValue);
    }

    [Fact]
    public void SdiCode_IsNotForIndividuals()
    {
        Assert.Equal(TenantFieldValidationErrorCodes.InvalidValue, TenantCreateValidator.ValidateSdiCode("M5UXCR1", TenantKind.Individual).ErrorCode);
        Assert.True(TenantCreateValidator.ValidateSdiCode("", TenantKind.Individual).IsValid);
    }

    [Theory]
    [InlineData(null, TenantKind.Business)]
    [InlineData("individual", TenantKind.Individual)]
    [InlineData("Business", TenantKind.Business)]
    public void Kind_ByName(string? value, TenantKind expected)
    {
        var errors = new Dictionary<string, string>();

        Assert.Equal(expected, TenantKinds.Parse(value, errors));
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("Alieno")]
    [InlineData("1")]
    public void Kind_Unknown_IsInvalidValue(string value)
    {
        var errors = new Dictionary<string, string>();

        TenantKinds.Parse(value, errors);

        Assert.Equal(TenantFieldValidationErrorCodes.InvalidValue, errors["kind"]);
    }
}
