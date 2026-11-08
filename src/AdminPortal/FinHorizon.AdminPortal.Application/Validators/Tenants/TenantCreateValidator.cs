using System.Net.Mail;
using System.Text.RegularExpressions;
using FinHorizon.AdminPortal.Application.Abstractions.Tenants;
using FinHorizon.AdminPortal.Application.Contracts.Tenants;
using FinHorizon.AdminPortal.Domain;

namespace FinHorizon.AdminPortal.Application.Validators.Tenants;

public sealed partial class TenantCreateValidator(ITenantUniquenessChecker uniquenessChecker)
{
    private const int CompanyNameMaxLength = 20;
    private const int PersonNameMaxLength = 50;

    // Synchronous format/required rules only (no uniqueness check).
    /// <param name="kind">For an individual the field is the full name: longer and never unique.</param>
    public TenantFieldValidationResult ValidateCompanyNameFormat(string? companyName, TenantKind kind = TenantKind.Business)
    {
        var maxLength = kind == TenantKind.Individual ? PersonNameMaxLength : CompanyNameMaxLength;
        var displayCompanyName = companyName?.Trim() ?? string.Empty;
        var normalizedCompanyName = TenantFieldNormalizer.NormalizeCompanyName(companyName);
        if (displayCompanyName.Length == 0 || normalizedCompanyName.Length == 0)
        {
            return TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.Required);
        }

        return displayCompanyName.Length > maxLength
            ? TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.MaxLengthExceeded, normalizedCompanyName)
            : TenantFieldValidationResult.Valid(normalizedCompanyName);
    }

    /// <param name="kind">For an individual the field holds the tax code (codice fiscale).</param>
    public TenantFieldValidationResult ValidateCompleteVatNumberFormat(string? completeVatNumber, TenantKind kind = TenantKind.Business)
    {
        var normalized = TenantFieldNormalizer.NormalizeCompleteVatNumber(completeVatNumber);
        if (normalized.Length == 0)
        {
            return TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.Required);
        }

        var valid = kind == TenantKind.Individual
            ? ItalianTaxCode.IsValid(normalized)
            : CompleteVatNumberRegex().IsMatch(normalized);
        return valid
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
        CancellationToken cancellationToken = default,
        TenantKind kind = TenantKind.Business)
    {
        var format = ValidateCompanyNameFormat(companyName, kind);
        if (!format.IsValid || kind == TenantKind.Individual) return format;
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
        CancellationToken cancellationToken = default,
        TenantKind kind = TenantKind.Business)
    {
        var format = ValidateCompleteVatNumberFormat(completeVatNumber, kind);
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

    /// <summary>
    /// Optional SDI recipient code: 7 characters, 6 for public administrations; stored uppercase.
    /// Individuals have none (their invoices go to the tax code with code 0000000).
    /// </summary>
    public static TenantFieldValidationResult ValidateSdiCode(string? sdiCode, TenantKind kind = TenantKind.Business)
    {
        var normalized = sdiCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (kind == TenantKind.Individual && normalized.Length > 0)
        {
            return TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.InvalidValue, normalized);
        }

        return normalized.Length == 0 || SdiCodeRegex().IsMatch(normalized)
            ? TenantFieldValidationResult.Valid(normalized)
            : TenantFieldValidationResult.Invalid(TenantFieldValidationErrorCodes.InvalidFormat, normalized);
    }

    [GeneratedRegex("^[A-Z0-9]{6,7}$", RegexOptions.CultureInvariant)]
    private static partial Regex SdiCodeRegex();

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
