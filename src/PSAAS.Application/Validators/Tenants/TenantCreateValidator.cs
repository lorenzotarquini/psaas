using System.Net.Mail;
using System.Text.RegularExpressions;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;

namespace PSAAS.Application.Validators.Tenants;

public sealed partial class TenantCreateValidator(ITenantUniquenessChecker uniquenessChecker)
{
    private const int CompanyNameMaxLength = 20;
    // Synchronous format/required rules only (no uniqueness check).
    public TenantFieldValidationResult ValidateCompanyNameFormat(string? companyName)
    {
        var displayCompanyName = companyName?.Trim() ?? string.Empty;
        var normalizedCompanyName = TenantFieldNormalizer.NormalizeCompanyName(companyName);
        if (displayCompanyName.Length == 0 || normalizedCompanyName.Length == 0)
        {
            return TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.Required);
        }

        return displayCompanyName.Length > CompanyNameMaxLength
            ? TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.MaxLengthExceeded, normalizedCompanyName)
            : TenantFieldValidationResult.Valid(normalizedCompanyName);
    }

    public TenantFieldValidationResult ValidateCompleteVatNumberFormat(string? completeVatNumber)
    {
        var normalized = TenantFieldNormalizer.NormalizeCompleteVatNumber(completeVatNumber);
        if (normalized.Length == 0)
        {
            return TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.Required);
        }

        return CompleteVatNumberRegex().IsMatch(normalized)
            ? TenantFieldValidationResult.Valid(normalized)
            : TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.InvalidFormat, normalized);
    }

    public TenantFieldValidationResult ValidateCertifiedEmailFormat(string? certifiedEmail)
    {
        var normalized = TenantFieldNormalizer.NormalizeCertifiedEmail(certifiedEmail);
        if (normalized.Length == 0)
        {
            return TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.Required);
        }

        return IsValidEmailAddress(normalized)
            ? TenantFieldValidationResult.Valid(normalized)
            : TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.InvalidFormat, normalized);
    }


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
        var format = ValidateCompanyNameFormat(companyName);
        if (!format.IsValid) return format;
        var normalizedCompanyName = TenantFieldNormalizer.NormalizeCompanyName(companyName);

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
        var format = ValidateCompleteVatNumberFormat(completeVatNumber);
        if (!format.IsValid) return format;
        var normalizedCompleteVatNumber = TenantFieldNormalizer.NormalizeCompleteVatNumber(completeVatNumber);

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
        var format = ValidateCertifiedEmailFormat(certifiedEmail);
        if (!format.IsValid) return format;
        var normalizedCertifiedEmail = TenantFieldNormalizer.NormalizeCertifiedEmail(certifiedEmail);

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

    // Shared with UserCreateService: one email rule across the portal.
    internal static bool IsValidEmailAddress(string email)
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
