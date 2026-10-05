using System.Net.Mail;
using System.Text.RegularExpressions;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;

namespace PSAAS.Application.Validators.Tenants;

public sealed partial class TenantCreateValidator(ITenantUniquenessChecker uniquenessChecker)
{
    private const int CompanyNameMaxLength = 20;

    public async Task<TenantFieldValidationResult> ValidateCompanyNameAsync(
        string? companyName,
        CancellationToken cancellationToken = default)
    {
        return await ValidateCompanyNameAsync(companyName, excludedTenantId: null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TenantFieldValidationResult> ValidateCompanyNameAsync(
        string? companyName,
        Guid? excludedTenantId,
        CancellationToken cancellationToken = default)
    {
        var displayCompanyName = companyName?.Trim() ?? string.Empty;
        var normalizedCompanyName = TenantFieldNormalizer.NormalizeCompanyName(companyName);
        if (displayCompanyName.Length == 0 || normalizedCompanyName.Length == 0)
        {
            return TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.Required);
        }

        if (displayCompanyName.Length > CompanyNameMaxLength)
        {
            return TenantFieldValidationResult.Invalid(
                TenantFieldValidationErrorCodes.MaxLengthExceeded,
                normalizedCompanyName);
        }

        var isUnique = excludedTenantId.HasValue
            ? await uniquenessChecker
                .IsCompanyNameUniqueAsync(normalizedCompanyName, excludedTenantId.Value, cancellationToken)
                .ConfigureAwait(false)
            : await uniquenessChecker
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
        return await ValidateCompleteVatNumberAsync(completeVatNumber, excludedTenantId: null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TenantFieldValidationResult> ValidateCompleteVatNumberAsync(
        string? completeVatNumber,
        Guid? excludedTenantId,
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

        var isUnique = excludedTenantId.HasValue
            ? await uniquenessChecker
                .IsCompleteVatNumberUniqueAsync(normalizedCompleteVatNumber, excludedTenantId.Value, cancellationToken)
                .ConfigureAwait(false)
            : await uniquenessChecker
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
        return await ValidateCertifiedEmailAsync(certifiedEmail, excludedTenantId: null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TenantFieldValidationResult> ValidateCertifiedEmailAsync(
        string? certifiedEmail,
        Guid? excludedTenantId,
        CancellationToken cancellationToken = default)
    {
        var normalizedCertifiedEmail = TenantFieldNormalizer.NormalizeCertifiedEmail(certifiedEmail);
        if (normalizedCertifiedEmail.Length == 0)
        {
            return TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.Required);
        }

        if (!IsValidEmailAddress(normalizedCertifiedEmail))
        {
            return TenantFieldValidationResult.Invalid(
                TenantFieldValidationErrorCodes.InvalidFormat,
                normalizedCertifiedEmail);
        }

        var isUnique = excludedTenantId.HasValue
            ? await uniquenessChecker
                .IsCertifiedEmailUniqueAsync(normalizedCertifiedEmail, excludedTenantId.Value, cancellationToken)
                .ConfigureAwait(false)
            : await uniquenessChecker
                .IsCertifiedEmailUniqueAsync(normalizedCertifiedEmail, cancellationToken)
                .ConfigureAwait(false);

        return isUnique
            ? TenantFieldValidationResult.Valid(normalizedCertifiedEmail)
            : TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.NotUnique, normalizedCertifiedEmail);
    }

    [GeneratedRegex("^[A-Z]{2}[0-9]{11}$", RegexOptions.CultureInvariant)]
    private static partial Regex CompleteVatNumberRegex();

    private static bool IsValidEmailAddress(string email)
    {
        try
        {
            var parsed = new MailAddress(email);
            return parsed.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
