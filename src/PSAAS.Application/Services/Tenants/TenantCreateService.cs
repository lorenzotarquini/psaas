using NLog;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using PSAAS.Domain;

namespace PSAAS.Application.Services.Tenants;

/// <summary>
/// Creates a tenant, optionally with its profiles and productive units, in one transaction.
/// Every value is validated server-side; duplicates are reported per field.
/// </summary>
public sealed class TenantCreateService(
    TenantCreateValidator validator,
    TenantAddressValidator addressValidator,
    ITenantWriteRepository tenants,
    IAuditLog audit,
    TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const string Country = "IT";

    public async Task<TenantCreateResult> CreateAsync(TenantCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string>();

        var completeVatNumber = await ValidateUniqueFieldsAsync(request, errors, cancellationToken);
        var street = Trim(request.Street);
        var city = await addressValidator.ValidateAsync(street, Trim(request.City), Trim(request.PostalCode), errors, cancellationToken);
        var sdiCode = TenantCreateValidator.ValidateSdiCode(request.SdiCode);
        if (!sdiCode.IsValid)
        {
            errors["sdiCode"] = sdiCode.ErrorCode!;
        }

        var profiles = request.Profiles ?? [];
        var units = request.ProductiveUnits ?? [];
        TenantOrganizationRules.ValidateNames(profiles.Select(profile => profile.Name).ToArray(), "profiles", errors);
        TenantOrganizationRules.ValidateNames(units.Select(unit => unit.Name).ToArray(), "productiveUnits", errors);
        var profileKeys = TenantOrganizationRules.ValidateProfileKeys(profiles.Select(profile => profile.Key).ToArray(), errors);
        TenantOrganizationRules.ValidateUnitProfileKeys(units.Select(unit => unit.ProfileKeys).ToArray(), profileKeys, errors);

        if (errors.Count > 0)
        {
            Logger.Info("Tenant creation rejected: {ValidationErrors}", errors);
            return new TenantCreateResult(null, errors);
        }

        var tenant = Tenant.Create(
            Trim(request.CompanyName),
            completeVatNumber[2..],
            completeVatNumber,
            Trim(request.CertifiedEmail),
            new Address(street, city, Trim(request.PostalCode), Country),
            request.Draft ? TenantStatus.Onboarding : TenantStatus.Active,
            new TenantCommercialInfo(NullIfEmpty(sdiCode.NormalizedValue), NullIfEmpty(request.AccountManager)),
            clock.GetUtcNow());
        // Profiles without a key are never referenced, so any unique temporary id works for them.
        var temporaryIds = profiles.Select(profile => NullIfEmpty(profile.Key) ?? Guid.NewGuid().ToString()).ToArray();

        try
        {
            await tenants.AddWithProfilesAndProductiveUnitsAsync(
                tenant,
                profiles.Select((profile, i) => new TenantProfileCreateModel(
                    temporaryIds[i], Trim(profile.Name), NullIfEmpty(profile.Description))).ToArray(),
                units.Select(unit => new TenantProductiveUnitCreateModel(
                    Trim(unit.Name),
                    NullIfEmpty(unit.Description),
                    [],
                    (unit.ProfileKeys ?? []).Select(Trim).ToArray())).ToArray(),
                cancellationToken);
        }
        catch (TenantUniquenessConflictException)
        {
            // A concurrent insert won the unique-index race: report which fields are now taken.
            await ValidateUniqueFieldsAsync(request, errors, cancellationToken);
            if (errors.Count == 0)
            {
                Logger.Error("Tenant {TenantId} hit a unique conflict that no field explains", tenant.Id);
                throw;
            }

            Logger.Warn("Tenant creation lost a concurrent duplicate race: {ValidationErrors}", errors);
            return new TenantCreateResult(null, errors);
        }

        await audit.RecordAsync(
            new AuditRecord(
                request.Draft ? AuditActions.TenantOnboardingStarted : AuditActions.TenantCreated,
                AuditTargets.Tenant,
                tenant.Id.ToString(),
                tenant.CompanyName,
                tenant.Id.ToString(),
                new { profiles = profiles.Count, productiveUnits = units.Count }),
            cancellationToken);
        Logger.Info(
            "Created tenant {TenantId} with {ProfileCount} profiles and {ProductiveUnitCount} productive units",
            tenant.Id,
            profiles.Count,
            units.Count);
        return new TenantCreateResult(tenant.Id, errors);
    }

    private async Task<string> ValidateUniqueFieldsAsync(
        TenantCreateRequest request,
        Dictionary<string, string> errors,
        CancellationToken cancellationToken)
    {
        AddError(errors, "companyName", await validator.ValidateCompanyNameAsync(request.CompanyName, cancellationToken));
        var completeVatNumber = await validator.ValidateCompleteVatNumberAsync(request.CompleteVatNumber, cancellationToken);
        AddError(errors, "completeVatNumber", completeVatNumber);
        AddError(errors, "certifiedEmail", await validator.ValidateCertifiedEmailAsync(request.CertifiedEmail, cancellationToken));
        return completeVatNumber.NormalizedValue;
    }

    private static void AddError(Dictionary<string, string> errors, string field, TenantFieldValidationResult result)
    {
        if (!result.IsValid)
        {
            errors[field] = result.ErrorCode!;
        }
    }

    private static string Trim(string? value) => TenantOrganizationRules.Trim(value);

    private static string? NullIfEmpty(string? value) => TenantOrganizationRules.NullIfEmpty(value);
}
