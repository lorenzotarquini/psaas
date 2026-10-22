using NLog;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Audit;
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
    ITenantWriteRepository tenants,
    IAuditLog audit)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<TenantEditResult> UpdateAsync(
        Guid tenantId,
        TenantRegistryUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var before = await reader.GetByIdAsync(tenantId, cancellationToken);
        if (before is null)
        {
            Logger.Info("Registry update of missing tenant {TenantId}", tenantId);
            return TenantEditResult.NotFound;
        }

        var errors = new Dictionary<string, string>();
        var completeVatNumber = await ValidateUniqueFieldsAsync(tenantId, request, errors, cancellationToken);
        var street = TenantOrganizationRules.Trim(request.Street);
        var postalCode = TenantOrganizationRules.Trim(request.PostalCode);
        var (city, province) = await addressValidator.ValidateAsync(
            street, TenantOrganizationRules.Trim(request.City), postalCode, errors, cancellationToken);
        var sdiCode = TenantCreateValidator.ValidateSdiCode(request.SdiCode);
        if (!sdiCode.IsValid)
        {
            errors["sdiCode"] = sdiCode.ErrorCode!;
        }
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
            new Address(street, city, postalCode, TenantCreateService.Country, province),
            before.Lifecycle,
            new TenantCommercialInfo(
                TenantOrganizationRules.NullIfEmpty(sdiCode.NormalizedValue),
                TenantOrganizationRules.NullIfEmpty(request.AccountManager)));
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

        var changes = Changes(before, tenant);
        if (changes.Count > 0)
        {
            await audit.RecordAsync(
                new AuditRecord(
                    AuditActions.TenantRegistryUpdated,
                    AuditTargets.Tenant,
                    tenantId.ToString(),
                    tenant.CompanyName,
                    tenantId.ToString(),
                    new RegistryChanges(changes)),
                cancellationToken);
        }

        Logger.Info("Updated the registry of tenant {TenantId}: {ChangedFields}", tenantId, changes.Keys);
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

    /// <summary>The fields that really changed, with before and after (EXP-10).</summary>
    private static Dictionary<string, AuditChange> Changes(Tenant before, Tenant after)
    {
        var pairs = new (string Field, string? Before, string? After)[]
        {
            ("companyName", before.CompanyName, after.CompanyName),
            ("completeVatNumber", before.CompleteVatNumber, after.CompleteVatNumber),
            ("certifiedEmail", before.CertifiedEmail, after.CertifiedEmail),
            ("street", before.Address.street, after.Address.street),
            ("city", before.Address.city, after.Address.city),
            ("postalCode", before.Address.postalCode, after.Address.postalCode),
            ("sdiCode", before.Commercial.SdiCode, after.Commercial.SdiCode),
            ("accountManager", before.Commercial.AccountManager, after.Commercial.AccountManager),
        };
        return pairs.Where(pair => pair.Before != pair.After).ToDictionary(pair => pair.Field, pair => new AuditChange(pair.Before, pair.After));
    }

    private static void AddError(Dictionary<string, string> errors, string field, TenantFieldValidationResult result)
    {
        if (!result.IsValid)
        {
            errors[field] = result.ErrorCode!;
        }
    }
}
