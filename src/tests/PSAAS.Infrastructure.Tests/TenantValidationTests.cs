using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using Xunit;

namespace PSAAS.Infrastructure.Tests;

public sealed class TenantValidationTests
{
    [Theory]
    [InlineData(" Acme S.r.l. ", "acmesrl")]
    [InlineData("Müller & Figli 123", "müllerfigli123")]
    public void NormalizeCompanyName_RemovesSeparatorsAndSymbols(string value, string expected)
    {
        var normalized = TenantFieldNormalizer.NormalizeCompanyName(value);

        Assert.Equal(expected, normalized);
    }

    [Fact]
    public async Task ValidateCompleteVatNumberAsync_NormalizesUppercaseAndChecksUniqueness()
    {
        var validator = new TenantCreateValidator(new InMemoryTenantUniquenessChecker());

        var result = await validator.ValidateCompleteVatNumberAsync(" it12345678901 ");

        Assert.True(result.IsValid);
        Assert.Equal("IT12345678901", result.NormalizedValue);
    }

    [Fact]
    public async Task ValidateCompleteVatNumberAsync_RejectsInvalidFormat()
    {
        var validator = new TenantCreateValidator(new InMemoryTenantUniquenessChecker());

        var result = await validator.ValidateCompleteVatNumberAsync("IT123");

        Assert.False(result.IsValid);
        Assert.Equal(TenantFieldValidationErrorCodes.InvalidFormat, result.ErrorCode);
    }

    [Fact]
    public async Task ValidateCertifiedEmailAsync_ChecksCaseInsensitiveUniqueness()
    {
        var validator = new TenantCreateValidator(
            new InMemoryTenantUniquenessChecker(
                normalizedCertifiedEmails: new HashSet<string>(StringComparer.Ordinal)
                {
                    "PEC@EXAMPLE.TEST"
                }));

        var result = await validator.ValidateCertifiedEmailAsync("pec@example.test");

        Assert.False(result.IsValid);
        Assert.Equal(TenantFieldValidationErrorCodes.NotUnique, result.ErrorCode);
        Assert.Equal("PEC@EXAMPLE.TEST", result.NormalizedValue);
    }

    private sealed class InMemoryTenantUniquenessChecker(
        IReadOnlySet<string>? normalizedCompanyNames = null,
        IReadOnlySet<string>? normalizedCompleteVatNumbers = null,
        IReadOnlySet<string>? normalizedCertifiedEmails = null) : ITenantUniquenessChecker
    {
        public Task<bool> IsCompanyNameUniqueAsync(
            string normalizedCompanyName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(!(normalizedCompanyNames?.Contains(normalizedCompanyName) ?? false));
        }

        public Task<bool> IsCompleteVatNumberUniqueAsync(
            string normalizedCompleteVatNumber,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(!(normalizedCompleteVatNumbers?.Contains(normalizedCompleteVatNumber) ?? false));
        }

        public Task<bool> IsCertifiedEmailUniqueAsync(
            string normalizedCertifiedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(!(normalizedCertifiedEmails?.Contains(normalizedCertifiedEmail) ?? false));
        }
    }
}
