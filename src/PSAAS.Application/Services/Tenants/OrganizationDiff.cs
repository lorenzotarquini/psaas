using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;

namespace PSAAS.Application.Services.Tenants;

/// <summary>
/// What an organization save (UC5) changed, by name, for the audit log and the tenant history:
/// "2 profili, 1 unità" (mock 1b) and the details behind it.
/// </summary>
public sealed record OrganizationDiff(
    IReadOnlyList<string> ProfilesAdded,
    IReadOnlyList<string> ProfilesRemoved,
    IReadOnlyList<AuditRename> ProfilesRenamed,
    IReadOnlyList<string> UnitsAdded,
    IReadOnlyList<string> UnitsRemoved,
    IReadOnlyList<AuditRename> UnitsRenamed,
    int AccessChanges)
{
    public bool IsEmpty =>
        ProfilesAdded.Count + ProfilesRemoved.Count + ProfilesRenamed.Count
        + UnitsAdded.Count + UnitsRemoved.Count + UnitsRenamed.Count + AccessChanges == 0;

    public static OrganizationDiff Between(TenantStructureReadModel before, TenantOrganizationRequest after)
    {
        var profiles = after.Profiles ?? [];
        var units = after.ProductiveUnits ?? [];
        var savedProfiles = before.Profiles.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var savedUnits = before.ProductiveUnits.ToDictionary(u => u.Id);
        var keptProfileIds = profiles.Select(p => TenantOrganizationRules.Trim(p.Id)).ToHashSet(StringComparer.Ordinal);
        var keptUnitIds = units.Where(u => u.Id is not null).Select(u => u.Id!.Value).ToHashSet();

        // Access between a kept profile and a kept unit, before and after; links of new items count with their addition.
        var profileIdByKey = profiles
            .Where(p => !string.IsNullOrWhiteSpace(p.Key) && !string.IsNullOrWhiteSpace(p.Id))
            .ToDictionary(p => p.Key!.Trim(), p => p.Id!.Trim(), StringComparer.Ordinal);
        var accessChanges = 0;
        foreach (var unit in units.Where(u => u.Id is not null && savedUnits.ContainsKey(u.Id.Value)))
        {
            var now = (unit.ProfileKeys ?? []).Select(k => profileIdByKey.GetValueOrDefault(k.Trim())).OfType<string>().ToHashSet();
            var then = savedUnits[unit.Id!.Value].ProfileIds.Where(keptProfileIds.Contains).ToHashSet();
            accessChanges += now.Count + then.Count - 2 * now.Intersect(then).Count();
        }

        return new OrganizationDiff(
            profiles.Where(p => string.IsNullOrWhiteSpace(p.Id)).Select(p => TenantOrganizationRules.Trim(p.Name)).ToArray(),
            before.Profiles.Where(p => !keptProfileIds.Contains(p.Id)).Select(p => p.Name).ToArray(),
            profiles
                .Where(p => !string.IsNullOrWhiteSpace(p.Id) && savedProfiles.TryGetValue(p.Id.Trim(), out var saved)
                    && saved.Name != TenantOrganizationRules.Trim(p.Name))
                .Select(p => new AuditRename(savedProfiles[p.Id!.Trim()].Name, TenantOrganizationRules.Trim(p.Name)))
                .ToArray(),
            units.Where(u => u.Id is null).Select(u => TenantOrganizationRules.Trim(u.Name)).ToArray(),
            before.ProductiveUnits.Where(u => !keptUnitIds.Contains(u.Id)).Select(u => u.Name).ToArray(),
            units
                .Where(u => u.Id is not null && savedUnits.TryGetValue(u.Id.Value, out var saved)
                    && saved.Name != TenantOrganizationRules.Trim(u.Name))
                .Select(u => new AuditRename(savedUnits[u.Id!.Value].Name, TenantOrganizationRules.Trim(u.Name)))
                .ToArray(),
            accessChanges);
    }
}

public sealed record AuditRename(string Before, string After);
