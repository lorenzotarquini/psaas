using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using Xunit;

namespace PSAAS.Application.Tests.Validators;

/// <summary>Derived from UC3-S3, UC3-S4, UC3-S5 (anagrafica rules) and UC4-S2 (a tenant may keep its own values).</summary>
public sealed class TenantCreateValidatorTests
{
    private readonly FakeUniquenessChecker _uniqueness = new();

    private TenantCreateValidator Validator => new(_uniqueness);

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("...")]
    public async Task CompanyName_MissingOrWithoutLettersOrDigits_IsRequired(string? companyName)
    {
        var result = await Validator.ValidateCompanyNameAsync(companyName);

        Assert.Equal(TenantFieldValidationErrorCodes.Required, result.ErrorCode);
    }

    [Fact]
    public async Task CompanyName_AtMostTwentyCharacters()
    {
        Assert.True((await Validator.ValidateCompanyNameAsync(new string('A', 20))).IsValid);
        Assert.Equal(
            TenantFieldValidationErrorCodes.MaxLengthExceeded,
            (await Validator.ValidateCompanyNameAsync(new string('A', 21))).ErrorCode);
    }

    [Fact]
    public async Task CompanyName_ComparedIgnoringCaseSpacesAndPunctuation()
    {
        _uniqueness.Taken["alfasrl"] = Guid.NewGuid();

        var result = await Validator.ValidateCompanyNameAsync("ALFA s.r.l.");

        Assert.Equal(TenantFieldValidationErrorCodes.NotUnique, result.ErrorCode);
    }

    [Fact]
    public async Task CompleteVatNumber_IsNormalizedToUppercase()
    {
        var result = await Validator.ValidateCompleteVatNumberAsync(" it12345678901 ");

        Assert.True(result.IsValid);
        Assert.Equal("IT12345678901", result.NormalizedValue);
    }

    [Theory]
    [InlineData("12345", "InvalidFormat")]
    [InlineData("IT1234567890", "InvalidFormat")]
    [InlineData("IT1234567890A", "InvalidFormat")]
    [InlineData("1T12345678901", "InvalidFormat")]
    [InlineData("", "Required")]
    public async Task CompleteVatNumber_IsTwoLettersAndElevenDigits(string value, string expected)
    {
        Assert.Equal(expected, (await Validator.ValidateCompleteVatNumberAsync(value)).ErrorCode);
    }

    [Fact]
    public async Task CompleteVatNumber_AlreadyUsed_IsNotUnique()
    {
        _uniqueness.Taken["IT12345678901"] = Guid.NewGuid();

        var result = await Validator.ValidateCompleteVatNumberAsync("it12345678901");

        Assert.Equal(TenantFieldValidationErrorCodes.NotUnique, result.ErrorCode);
    }

    [Theory]
    [InlineData("non-una-pec", "InvalidFormat")]
    [InlineData("Mario <mario@pec.it>", "InvalidFormat")]
    [InlineData("", "Required")]
    public async Task CertifiedEmail_MustBeAPlainAddress(string value, string expected)
    {
        Assert.Equal(expected, (await Validator.ValidateCertifiedEmailAsync(value)).ErrorCode);
    }

    [Fact]
    public async Task CertifiedEmail_ComparedIgnoringCase()
    {
        _uniqueness.Taken["ALFA@PEC.IT"] = Guid.NewGuid();

        var result = await Validator.ValidateCertifiedEmailAsync("alfa@pec.it");

        Assert.Equal(TenantFieldValidationErrorCodes.NotUnique, result.ErrorCode);
        Assert.Equal("ALFA@PEC.IT", result.NormalizedValue);
    }

    [Fact]
    public async Task ExcludedTenant_MayKeepItsOwnValues()
    {
        var alfa = Guid.NewGuid();
        _uniqueness.Taken["alfasrl"] = alfa;
        _uniqueness.Taken["IT12345678901"] = alfa;
        _uniqueness.Taken["ALFA@PEC.IT"] = alfa;

        Assert.True((await Validator.ValidateCompanyNameAsync("Alfa Srl", alfa)).IsValid);
        Assert.True((await Validator.ValidateCompleteVatNumberAsync("IT12345678901", alfa)).IsValid);
        Assert.True((await Validator.ValidateCertifiedEmailAsync("alfa@pec.it", alfa)).IsValid);
        Assert.False((await Validator.ValidateCompanyNameAsync("Alfa Srl", Guid.NewGuid())).IsValid);
    }

    [Theory]
    [InlineData(null, true, "")]
    [InlineData(" m5uxcr1 ", true, "M5UXCR1")]
    [InlineData("UFABC1", true, "UFABC1")]
    [InlineData("ABC", false, "ABC")]
    [InlineData("M5UX-R1", false, "M5UX-R1")]
    public void UC14_S5_SdiCode_IsOptional_SixOrSevenLettersOrDigits(string? value, bool valid, string normalized)
    {
        var result = TenantCreateValidator.ValidateSdiCode(value);

        Assert.Equal((valid, normalized), (result.IsValid, result.NormalizedValue));
    }
}
