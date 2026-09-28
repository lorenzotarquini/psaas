using System.Text.RegularExpressions;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;

namespace PSAAS.Application.Validators.Tenants;

public sealed partial class TenantCreateValidator(ITenantUniquenessChecker uniquenessChecker)
{
    public async Task<TenantFieldValidationResult> ValidateCompanyNameAsync(
        string? companyName,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyName = TenantFieldNormalizer.NormalizeCompanyName(companyName);
        if (normalizedCompanyName.Length == 0)
        {
            return TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.Required);
        }

        var isUnique = await uniquenessChecker
            .IsCompanyNameUniqueAsync(normalizedCompanyName, cancellationToken)
            .ConfigureAwait(false);

        return isUnique
            ? TenantFieldValidationResult.Valid(normalizedCompanyName)
            : TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.NotUnique, normalizedCompanyName);
    }

    public async Task<TenantFieldValidationResult> ValidateCompleteVatNumberAsync(
        string? completeVatNumber,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompleteVatNumber = TenantFieldNormalizer.NormalizeCompleteVatNumber(completeVatNumber);
        if (normalizedCompleteVatNumber.Length == 0)
        {
            return TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.Required);
        }

        if (!CompleteVatNumberRegex().IsMatch(normalizedCompleteVatNumber))
        {
            return TenantFieldValidationResult.Invalid(
                TenantFieldValidationErrorCodes.InvalidFormat,
                normalizedCompleteVatNumber);
        }

        var isUnique = await uniquenessChecker
            .IsCompleteVatNumberUniqueAsync(normalizedCompleteVatNumber, cancellationToken)
            .ConfigureAwait(false);

        return isUnique
            ? TenantFieldValidationResult.Valid(normalizedCompleteVatNumber)
            : TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.NotUnique, normalizedCompleteVatNumber);
    }

    public async Task<TenantFieldValidationResult> ValidateCertifiedEmailAsync(
        string? certifiedEmail,
        CancellationToken cancellationToken = default)
    {
        var normalizedCertifiedEmail = TenantFieldNormalizer.NormalizeCertifiedEmail(certifiedEmail);
        if (normalizedCertifiedEmail.Length == 0)
        {
            return TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.Required);
        }

        var isUnique = await uniquenessChecker
            .IsCertifiedEmailUniqueAsync(normalizedCertifiedEmail, cancellationToken)
            .ConfigureAwait(false);

        return isUnique
            ? TenantFieldValidationResult.Valid(normalizedCertifiedEmail)
            : TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.NotUnique, normalizedCertifiedEmail);
    }

    [GeneratedRegex("^[A-Z]{2}[0-9]{11}$", RegexOptions.CultureInvariant)]
    private static partial Regex CompleteVatNumberRegex();
}
