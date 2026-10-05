using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using PSAAS.Domain;

namespace PSAAS.AdminPortal.Features.Tenants.Services;

/// <summary>
/// Feature-scoped application service that normalizes and validates the tenant
/// creation request server-side, builds the domain aggregate and persists it
/// through the write repository. Client-side validation is never trusted.
/// </summary>
public sealed class TenantCreateService(
    TenantCreateValidator tenantCreateValidator,
    ITenantWriteRepository tenantWriteRepository) : ITenantCreateService
{
    public async Task<TenantCreateResult> CreateTenantAsync(
        TenantCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var companyName = await tenantCreateValidator
            .ValidateCompanyNameAsync(request.CompanyName, cancellationToken)
            .ConfigureAwait(false);
        var completeVatNumber = await tenantCreateValidator
            .ValidateCompleteVatNumberAsync(request.CompleteVatNumber, cancellationToken)
            .ConfigureAwait(false);
        var certifiedEmail = await tenantCreateValidator
            .ValidateCertifiedEmailAsync(request.CertifiedEmail, cancellationToken)
            .ConfigureAwait(false);

        var street = Trim(request.Street);
        var city = Trim(request.City);
        var postalCode = Trim(request.PostalCode);
        var country = Trim(request.Country);

        if (!companyName.IsValid || !completeVatNumber.IsValid || !certifiedEmail.IsValid
            || street.Length == 0 || city.Length == 0 || postalCode.Length == 0 || country.Length == 0)
        {
            return TenantCreateResult.Invalid(
                companyName.ErrorCode,
                completeVatNumber.ErrorCode,
                certifiedEmail.ErrorCode,
                ToRequiredErrorCode(street),
                ToRequiredErrorCode(city),
                ToRequiredErrorCode(postalCode),
                ToRequiredErrorCode(country));
        }

        // The form has a single VAT field carrying the complete VAT number
        // (2-letter country prefix plus 11 digits): the 11-digit national part
        // is derived here to populate the domain VatNumber property.
        var tenant = Tenant.Create(
            Trim(request.CompanyName),
            completeVatNumber.NormalizedValue[2..],
            completeVatNumber.NormalizedValue,
            Trim(request.CertifiedEmail),
            new Address(street, city, postalCode, country));

        try
        {
            await tenantWriteRepository
                .AddAsync(tenant, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TenantUniquenessConflictException)
        {
            return await ResolveUniquenessConflictAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return TenantCreateResult.Failed(TenantCreateErrorCodes.SaveFailed);
        }

        return TenantCreateResult.Created();
    }

    // A concurrent insert won the unique-index race: re-check every unique
    // value to report which fields are now duplicated, so the UI can surface
    // a per-field message instead of a generic save failure.
    private async Task<TenantCreateResult> ResolveUniquenessConflictAsync(
        TenantCreateRequest request,
        CancellationToken cancellationToken)
    {
        var companyName = await tenantCreateValidator
            .ValidateCompanyNameAsync(request.CompanyName, cancellationToken)
            .ConfigureAwait(false);
        var completeVatNumber = await tenantCreateValidator
            .ValidateCompleteVatNumberAsync(request.CompleteVatNumber, cancellationToken)
            .ConfigureAwait(false);
        var certifiedEmail = await tenantCreateValidator
            .ValidateCertifiedEmailAsync(request.CertifiedEmail, cancellationToken)
            .ConfigureAwait(false);

        if (companyName.IsValid && completeVatNumber.IsValid && certifiedEmail.IsValid)
        {
            // The conflict could not be attributed to any field: fall back to
            // a generic, non-destructive save failure.
            return TenantCreateResult.Failed(TenantCreateErrorCodes.SaveFailed);
        }

        return TenantCreateResult.Invalid(
            companyName.ErrorCode,
            completeVatNumber.ErrorCode,
            certifiedEmail.ErrorCode);
    }

    private static string? ToRequiredErrorCode(string value) =>
        value.Length == 0 ? TenantFieldValidationErrorCodes.Required : null;

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;
}
