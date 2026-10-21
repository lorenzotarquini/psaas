using PSAAS.Application.Validators.Tenants;
using Xunit;

namespace PSAAS.Application.Tests.Validators;

/// <summary>Derived from UC1-S2 and UC3-S5: names are compared ignoring case, spaces and punctuation.</summary>
public sealed class TenantFieldNormalizerTests
{
    [Theory]
    [InlineData(" Acme S.r.l. ", "acmesrl")]
    [InlineData("ALFA s.r.l.", "alfasrl")]
    [InlineData("Müller & Figli 123", "müllerfigli123")]
    [InlineData("...", "")]
    [InlineData(null, "")]
    public void CompanyName_KeepsOnlyLowercaseLettersAndDigits(string? value, string expected)
    {
        Assert.Equal(expected, TenantFieldNormalizer.NormalizeCompanyName(value));
    }

    [Theory]
    [InlineData(" it12345678901 ", "IT12345678901")]
    [InlineData(null, "")]
    public void CompleteVatNumber_IsTrimmedAndUppercase(string? value, string expected)
    {
        Assert.Equal(expected, TenantFieldNormalizer.NormalizeCompleteVatNumber(value));
    }

    [Theory]
    [InlineData(" Alfa@Pec.it ", "ALFA@PEC.IT")]
    [InlineData(null, "")]
    public void CertifiedEmail_IsTrimmedAndUppercase(string? value, string expected)
    {
        Assert.Equal(expected, TenantFieldNormalizer.NormalizeCertifiedEmail(value));
    }
}
