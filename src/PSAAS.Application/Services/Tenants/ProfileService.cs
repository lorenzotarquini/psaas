using NLog;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Profiles;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;

namespace PSAAS.Application.Services.Tenants;

/// <summary>
/// UC9: creates and deletes single profiles. Both are an organization change (UC5): same rules, same audit, and a
/// deleted profile is taken off its users.
/// </summary>
public sealed class ProfileService(
    ITenantReadRepository reader,
    IProfileReadRepository profiles,
    TenantOrganizationService organization)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string NewProfileKey = "new";

    public async Task<ProfileCreateResult> CreateAsync(ProfileCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TenantId is not { } tenantId)
        {
            return Rejected("tenantId", TenantFieldValidationErrorCodes.Required);
        }

        var tenant = await reader.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            Logger.Info("Profile creation for missing tenant {TenantId}", tenantId);
            return Rejected("tenantId", TenantFieldValidationErrorCodes.InvalidValue);
        }

        var current = await reader.GetProfilesAndProductiveUnitsAsync(tenantId.ToString(), cancellationToken);
        var kept = Unchanged(current);
        var newProfile = new TenantOrganizationProfileRequest(null, NewProfileKey, request.Name, request.Description);
        var result = await organization.SaveAsync(
            tenantId,
            kept with { Profiles = [.. kept.Profiles!, newProfile] },
            cancellationToken);
        if (!result.Found)
        {
            return Rejected("tenantId", TenantFieldValidationErrorCodes.InvalidValue);
        }

        if (result.Errors.Count > 0)
        {
            // Only the new profile can be wrong: report its fields without the position in the organization.
            var prefix = $"profiles[{current.Profiles.Count}].";
            return new ProfileCreateResult(null, result.Errors.ToDictionary(
                error => error.Key.StartsWith(prefix, StringComparison.Ordinal) ? error.Key[prefix.Length..] : error.Key,
                error => error.Value));
        }

        var name = TenantOrganizationRules.Trim(request.Name);
        var saved = await reader.GetProfilesAndProductiveUnitsAsync(tenantId.ToString(), cancellationToken);
        var created = saved.Profiles.Single(profile => profile.Name == name && current.Profiles.All(old => old.Id != profile.Id));
        Logger.Info("Created profile {ProfileId} in tenant {TenantId}", created.Id, tenantId);
        return new ProfileCreateResult(
            new ProfileReadModel(created.Id, tenantId.ToString(), tenant.CompanyName, created.Name, created.Description),
            new Dictionary<string, string>());
    }

    /// <returns><see langword="false" /> when the profile does not exist.</returns>
    public async Task<bool> DeleteAsync(string profileId, CancellationToken cancellationToken = default)
    {
        var found = await profiles.GetWithTenantAsync([profileId], cancellationToken);
        if (found.Count == 0 || !Guid.TryParse(found[0].TenantId, out var tenantId))
        {
            Logger.Info("Deletion of missing profile {ProfileId}", profileId);
            return false;
        }

        var current = await reader.GetProfilesAndProductiveUnitsAsync(tenantId.ToString(), cancellationToken);
        var kept = Unchanged(current);
        var result = await organization.SaveAsync(
            tenantId,
            new TenantOrganizationRequest(
                kept.Profiles!.Where(profile => profile.Id != profileId).ToArray(),
                // The units that it opened lose it (UC5-R3 seen from the profile).
                kept.ProductiveUnits!.Select(unit => unit with { ProfileKeys = unit.ProfileKeys!.Where(key => key != profileId).ToArray() }).ToArray()),
            cancellationToken);
        if (!result.Succeeded)
        {
            // The profile was read a moment ago: only a concurrent deletion of the tenant gets here.
            Logger.Warn("Deletion of profile {ProfileId} found no tenant {TenantId}", profileId, tenantId);
            return false;
        }

        Logger.Info("Deleted profile {ProfileId} of tenant {TenantId}", profileId, tenantId);
        return true;
    }

    // The organization as it is, in the UC5 request shape: existing profiles are keyed by their own id.
    private static TenantOrganizationRequest Unchanged(TenantStructureReadModel current) => new(
        current.Profiles.Select(profile => new TenantOrganizationProfileRequest(profile.Id, profile.Id, profile.Name, profile.Description)).ToArray(),
        current.ProductiveUnits.Select(unit => new TenantOrganizationUnitRequest(unit.Id, unit.Name, unit.Description, unit.ProfileIds)).ToArray());

    private static ProfileCreateResult Rejected(string field, string code) =>
        new(null, new Dictionary<string, string> { [field] = code });
}
