using NLog;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using PSAAS.Domain;

namespace PSAAS.Application.Services.Tenants;

/// <summary>
/// UC4: updates the registry of an existing tenant with the UC3 rules; uniqueness excludes the tenant itself,
/// so saving its own unchanged values is not a duplicate.
/// </summary>
public sealed class TenantRegistryUpdateService(
    ITenantReadRepository reader,
    TenantCreateValidator validator,
    TenantAddressValidator addressValidator,
    ITenantWriteRepository tenants)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<TenantEditResult> UpdateAsync(
        Guid tenantId,
        TenantRegistryUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await reader.GetByIdAsync(tenantId, cancellationToken) is null)
        {
            Logger.Info("Registry update of missing tenant {TenantId}", tenantId);
            return TenantEditResult.NotFound;
        }

        var errors = new Dictionary<string, string>();
        var completeVatNumber = await ValidateUniqueFieldsAsync(tenantId, request, errors, cancellationToken);
        var street = TenantOrganizationRules.Trim(request.Street);
        var postalCode = TenantOrganizationRules.Trim(request.PostalCode);
        var city = await addressValidator.ValidateAsync(
            street, TenantOrganizationRules.Trim(request.City), postalCode, errors, cancellationToken);
        if (errors.Count > 0)
        {
            Logger.Info("Registry update of tenant {TenantId} rejected: {ValidationErrors}", tenantId, errors);
            return new TenantEditResult(true, errors);
        }

        var tenant = Tenant.Rehydrate(
            tenantId,
            TenantOrganizationRules.Trim(request.CompanyName),
            completeVatNumber[2..],
            completeVatNumber,
            TenantOrganizationRules.Trim(request.CertifiedEmail),
            new Address(street, city, postalCode, TenantCreateService.Country));
        try
        {
            if (!await tenants.UpdateAsync(tenant, cancellationToken))
            {
                return TenantEditResult.NotFound;
            }
        }
        catch (TenantUniquenessConflictException)
        {
            // Another administrator saved the same value meanwhile (UC3 flow 3b): report the fields.
            await ValidateUniqueFieldsAsync(tenantId, request, errors, cancellationToken);
            if (errors.Count == 0)
            {
                throw;
            }

            Logger.Warn("Registry update of tenant {TenantId} lost a concurrent duplicate race: {ValidationErrors}", tenantId, errors);
            return new TenantEditResult(true, errors);
        }

        Logger.Info("Updated the registry of tenant {TenantId}", tenantId);
        return new TenantEditResult(true, errors);
    }

    private async Task<string> ValidateUniqueFieldsAsync(
        Guid tenantId,
        TenantRegistryUpdateRequest request,
        Dictionary<string, string> errors,
        CancellationToken cancellationToken)
    {
        AddError(errors, "companyName", await validator.ValidateCompanyNameAsync(request.CompanyName, tenantId, cancellationToken));
        var completeVatNumber = await validator.ValidateCompleteVatNumberAsync(request.CompleteVatNumber, tenantId, cancellationToken);
        AddError(errors, "completeVatNumber", completeVatNumber);
        AddError(errors, "certifiedEmail", await validator.ValidateCertifiedEmailAsync(request.CertifiedEmail, tenantId, cancellationToken));
        return completeVatNumber.NormalizedValue;
    }

    private static void AddError(Dictionary<string, string> errors, string field, TenantFieldValidationResult result)
    {
        if (!result.IsValid)
        {
            errors[field] = result.ErrorCode!;
        }
    }
}
