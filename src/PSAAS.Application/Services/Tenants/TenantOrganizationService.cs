using NLog;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;

namespace PSAAS.Application.Services.Tenants;

/// <summary>
/// UC5: replaces the profiles and productive units of a tenant in one transaction. Profiles and units with an
/// id keep their identity (renames); ids must belong to the tenant. Removed profiles are taken off the users.
/// </summary>
public sealed class TenantOrganizationService(
    ITenantReadRepository reader,
    ITenantWriteRepository tenants,
    IUserDirectory users)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<TenantEditResult> SaveAsync(
        Guid tenantId,
        TenantOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenant = await reader.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            Logger.Info("Organization update of missing tenant {TenantId}", tenantId);
            return TenantEditResult.NotFound;
        }

        var current = await reader.GetProfilesAndProductiveUnitsAsync(tenantId.ToString(), cancellationToken);
        var profiles = request.Profiles ?? [];
        var units = request.ProductiveUnits ?? [];
        var errors = new Dictionary<string, string>();
        TenantOrganizationRules.ValidateNames(profiles.Select(profile => profile.Name).ToArray(), "profiles", errors);
        TenantOrganizationRules.ValidateNames(units.Select(unit => unit.Name).ToArray(), "productiveUnits", errors);
        var profileKeys = TenantOrganizationRules.ValidateProfileKeys(profiles.Select(profile => profile.Key).ToArray(), errors);
        TenantOrganizationRules.ValidateUnitProfileKeys(units.Select(unit => unit.ProfileKeys).ToArray(), profileKeys, errors);
        ValidateOwnIds(profiles.Select(profile => profile.Id).ToArray(), current.Profiles.Select(p => p.Id), "profiles", errors);
        ValidateOwnIds(units.Select(unit => unit.Id?.ToString()).ToArray(), current.ProductiveUnits.Select(u => u.Id.ToString()), "productiveUnits", errors);
        if (errors.Count > 0)
        {
            Logger.Info("Organization update of tenant {TenantId} rejected: {ValidationErrors}", tenantId, errors);
            return new TenantEditResult(true, errors);
        }

        // Profiles without a key are never referenced by a unit: any client id works for them.
        var clientIds = profiles.Select(_ => Guid.NewGuid()).ToArray();
        var clientIdByKey = profiles
            .Select((profile, i) => (Key: TenantOrganizationRules.Trim(profile.Key), ClientId: clientIds[i]))
            .Where(pair => pair.Key.Length > 0)
            .ToDictionary(pair => pair.Key, pair => pair.ClientId, StringComparer.Ordinal);
        var saved = await tenants.UpdateWithStructureAsync(
            tenant,
            profiles.Select((profile, i) => new TenantProfileEditModel(
                TenantOrganizationRules.NullIfEmpty(profile.Id),
                clientIds[i],
                TenantOrganizationRules.Trim(profile.Name),
                TenantOrganizationRules.NullIfEmpty(profile.Description))).ToArray(),
            units.Select(unit => new TenantProductiveUnitEditModel(
                unit.Id,
                TenantOrganizationRules.Trim(unit.Name),
                TenantOrganizationRules.NullIfEmpty(unit.Description),
                (unit.ProfileKeys ?? []).Select(key => clientIdByKey[TenantOrganizationRules.Trim(key)]).Distinct().ToArray())).ToArray(),
            cancellationToken);
        if (!saved)
        {
            return TenantEditResult.NotFound;
        }

        var kept = profiles.Select(profile => TenantOrganizationRules.Trim(profile.Id)).ToHashSet(StringComparer.Ordinal);
        var removed = current.Profiles.Select(profile => profile.Id).Where(id => !kept.Contains(id)).ToArray();
        await RemoveFromUsersAsync(tenantId, removed, cancellationToken);
        Logger.Info(
            "Saved the organization of tenant {TenantId}: {ProfileCount} profiles, {UnitCount} units, {RemovedCount} profiles removed",
            tenantId,
            profiles.Count,
            units.Count,
            removed.Length);
        return new TenantEditResult(true, errors);
    }

    /// <summary>UC5-S6: an id that is not a profile or unit of this tenant (e.g. of another tenant) is InvalidValue.</summary>
    private static void ValidateOwnIds(
        IReadOnlyList<string?> ids,
        IEnumerable<string> ownIds,
        string collection,
        Dictionary<string, string> errors)
    {
        var own = ownIds.ToHashSet(StringComparer.Ordinal);
        for (var i = 0; i < ids.Count; i++)
        {
            var id = TenantOrganizationRules.Trim(ids[i]);
            if (id.Length > 0 && !own.Contains(id))
            {
                errors[$"{collection}[{i}].id"] = TenantFieldValidationErrorCodes.InvalidValue;
            }
        }
    }

    // UC5-R4. The organization is already saved: if Keycloak is down the users keep a dangling id, which every
    // read already omits (UC7-R3), so the save is not undone.
    private async Task RemoveFromUsersAsync(Guid tenantId, IReadOnlyCollection<string> removed, CancellationToken cancellationToken)
    {
        if (removed.Count == 0)
        {
            return;
        }

        try
        {
            await users.RemoveProfilesAsync(removed, cancellationToken);
        }
        catch (UserDirectoryUnavailableException exception)
        {
            Logger.Warn(exception, "Profiles {ProfileIds} of tenant {TenantId} removed, but not from the Keycloak users", removed, tenantId);
        }
    }
}
