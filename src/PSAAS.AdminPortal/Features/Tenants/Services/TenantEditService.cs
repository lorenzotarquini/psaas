using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using PSAAS.Domain;

namespace PSAAS.AdminPortal.Features.Tenants.Services;

/// <summary>
/// Feature-scoped application service for loading and updating an editable
/// tenant projection without exposing the domain model to the UI.
/// </summary>
public sealed class TenantEditService(
    TenantCreateValidator tenantCreateValidator,
    ITenantReadRepository tenantReadRepository,
    ITenantWriteRepository tenantWriteRepository) : ITenantEditService
{
    public async Task<TenantEditReadModel?> GetTenantForEditAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await tenantReadRepository.GetByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

        return tenant is null ? null : ToReadModel(tenant);
    }

    public async Task<TenantEditResult> UpdateTenantAsync(
        TenantEditRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Id == Guid.Empty)
        {
            return TenantEditResult.Failed(TenantEditErrorCodes.NotFound);
        }

        var validationResult = await ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (validationResult.InvalidResult is not null)
        {
            return validationResult.InvalidResult;
        }

        var updatedTenant = Tenant.Rehydrate(
            request.Id,
            Trim(request.CompanyName),
            validationResult.CompleteVatNumber!.NormalizedValue[2..],
            validationResult.CompleteVatNumber.NormalizedValue,
            Trim(request.CertifiedEmail),
            new Address(
                Trim(request.Street),
                Trim(request.City),
                Trim(request.PostalCode),
                Trim(request.Country)));

        try
        {
            var updated = await tenantWriteRepository
                .UpdateAsync(updatedTenant, cancellationToken)
                .ConfigureAwait(false);

            return updated
                ? TenantEditResult.Updated()
                : TenantEditResult.Failed(TenantEditErrorCodes.NotFound);
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
            return TenantEditResult.Failed(TenantEditErrorCodes.SaveFailed);
        }
    }

    private async Task<ValidationOutcome> ValidateAsync(
        TenantEditRequest request,
        CancellationToken cancellationToken)
    {
        var companyName = await tenantCreateValidator
            .ValidateCompanyNameAsync(request.CompanyName, request.Id, cancellationToken)
            .ConfigureAwait(false);
        var completeVatNumber = await tenantCreateValidator
            .ValidateCompleteVatNumberAsync(request.CompleteVatNumber, request.Id, cancellationToken)
            .ConfigureAwait(false);
        var certifiedEmail = await tenantCreateValidator
            .ValidateCertifiedEmailAsync(request.CertifiedEmail, request.Id, cancellationToken)
            .ConfigureAwait(false);

        var street = Trim(request.Street);
        var city = Trim(request.City);
        var postalCode = Trim(request.PostalCode);
        var country = Trim(request.Country);

        if (!companyName.IsValid || !completeVatNumber.IsValid || !certifiedEmail.IsValid
            || street.Length == 0 || city.Length == 0 || postalCode.Length == 0 || country.Length == 0)
        {
            return new ValidationOutcome(
                null,
                TenantEditResult.Invalid(
                    companyName.ErrorCode,
                    completeVatNumber.ErrorCode,
                    certifiedEmail.ErrorCode,
                    ToRequiredErrorCode(street),
                    ToRequiredErrorCode(city),
                    ToRequiredErrorCode(postalCode),
                    ToRequiredErrorCode(country)));
        }

        return new ValidationOutcome(completeVatNumber, null);
    }

    private async Task<TenantEditResult> ResolveUniquenessConflictAsync(
        TenantEditRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (validationResult.InvalidResult is null)
        {
            return TenantEditResult.Failed(TenantEditErrorCodes.SaveFailed);
        }

        return validationResult.InvalidResult;
    }

    private static TenantEditReadModel ToReadModel(Tenant tenant) =>
        new(
            tenant.Id,
            tenant.CompanyName,
            tenant.CompleteVatNumber,
            tenant.CertifiedEmail,
            tenant.Address.street,
            tenant.Address.city,
            tenant.Address.postalCode,
            tenant.Address.country);

    private static string? ToRequiredErrorCode(string value) =>
        value.Length == 0 ? TenantFieldValidationErrorCodes.Required : null;

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;

    private sealed record ValidationOutcome(
        TenantFieldValidationResult? CompleteVatNumber,
        TenantEditResult? InvalidResult);
}
