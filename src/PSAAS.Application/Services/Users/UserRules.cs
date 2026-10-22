using System.Globalization;
using NLog;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Contracts.Users;
using PSAAS.Application.Validators.Tenants;

namespace PSAAS.Application.Services.Users;

/// <summary>
/// The rules a user's values follow wherever they are written (UC6 creation, UC19 edit, UC20 import),
/// and the audit of a user change in the history of each of its tenants.
/// </summary>
public sealed class UserRules(
    IProfileReadRepository profiles,
    IUserDirectory directory,
    ISubscriptionRepository subscriptions,
    ITenantReadRepository tenants,
    IAuditLog audit)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string Trim(string? value) => value?.Trim() ?? string.Empty;

    public static void Required(Dictionary<string, string> errors, string field, string value)
    {
        if (value.Length == 0)
        {
            errors[field] = TenantFieldValidationErrorCodes.Required;
        }
    }

    /// <summary>UC6-R2: the email is also the username; Keycloak stores both lowercase.</summary>
    public static string ValidateEmail(string? value, Dictionary<string, string> errors)
    {
        var email = Trim(value).ToLowerInvariant();
        if (email.Length == 0)
        {
            errors["email"] = TenantFieldValidationErrorCodes.Required;
        }
        else if (!TenantCreateValidator.IsValidEmailAddress(email))
        {
            errors["email"] = TenantFieldValidationErrorCodes.InvalidFormat;
        }

        return email;
    }

    /// <summary>UC6-R3: any valid calendar date, "YYYY-MM-DD".</summary>
    public static DateOnly? ValidateBirthDate(string? value, Dictionary<string, string> errors)
    {
        var birthDate = Trim(value);
        if (birthDate.Length == 0)
        {
            errors["birthDate"] = TenantFieldValidationErrorCodes.Required;
            return null;
        }

        if (DateOnly.TryParseExact(birthDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        errors["birthDate"] = TenantFieldValidationErrorCodes.InvalidFormat;
        return null;
    }

    /// <summary>
    /// Every profile must exist (profileIds[i] = InvalidValue); then, if nothing else is wrong, no tenant may go over
    /// the users of its plan (EXP-02). Tenants in <paramref name="alreadyMember"/> are skipped: the user already counts there.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, UserProfileReadModel>> ValidateProfilesAsync(
        string[] profileIds,
        Dictionary<string, string> errors,
        IReadOnlySet<string>? alreadyMember,
        CancellationToken cancellationToken)
    {
        var existing = (await profiles.GetWithTenantAsync(profileIds, cancellationToken))
            .ToDictionary(profile => profile.Id, StringComparer.Ordinal);
        for (var i = 0; i < profileIds.Length; i++)
        {
            if (!existing.ContainsKey(profileIds[i]))
            {
                errors[$"profileIds[{i}]"] = TenantFieldValidationErrorCodes.InvalidValue;
            }
        }

        if (errors.Count == 0)
        {
            await CheckUserLimitsAsync(profileIds, existing, errors, alreadyMember ?? new HashSet<string>(), cancellationToken);
        }

        return existing;
    }

    // EXP-02: a tenant whose plan allows N users cannot get the N+1th; reported on its first chosen profile.
    private async Task CheckUserLimitsAsync(
        string[] profileIds,
        IReadOnlyDictionary<string, UserProfileReadModel> existing,
        Dictionary<string, string> errors,
        IReadOnlySet<string> alreadyMember,
        CancellationToken cancellationToken)
    {
        foreach (var tenantId in profileIds.Select(id => existing[id].TenantId).Distinct().Where(id => !alreadyMember.Contains(id)))
        {
            if (!Guid.TryParse(tenantId, out var id)) continue;
            var maxUsers = (await subscriptions.GetAsync(id, cancellationToken))?.MaxUsers;
            if (maxUsers is null) continue;
            var tenantProfiles = (await tenants.GetProfilesAndProductiveUnitsAsync(tenantId, cancellationToken)).Profiles.Select(p => p.Id).ToArray();
            if (await directory.CountUsersWithAnyAsync(tenantProfiles, cancellationToken) >= maxUsers)
            {
                var first = Array.FindIndex(profileIds, profileId => existing[profileId].TenantId == tenantId);
                errors[$"profileIds[{first}]"] = TenantFieldValidationErrorCodes.LimitExceeded;
                Logger.Info("Tenant {TenantId} reached its {MaxUsers} users", tenantId, maxUsers);
            }
        }
    }

    /// <summary>
    /// One event per tenant of the user's profiles, so each tenant history shows it (mock 1b); none: platform-wide.
    /// <paramref name="details"/> gets the profiles of that tenant (empty for the platform-wide event).
    /// </summary>
    public async Task RecordAsync(
        string action,
        UserReadModel user,
        Func<IReadOnlyList<UserProfileReadModel>, object?> details,
        CancellationToken cancellationToken)
    {
        var name = $"{user.FirstName} {user.LastName}";
        var byTenant = user.Profiles.GroupBy(profile => profile.TenantId).ToArray();
        if (byTenant.Length == 0)
        {
            await audit.RecordAsync(new AuditRecord(action, AuditTargets.User, user.Id, name, null, details([])), cancellationToken);
            return;
        }

        foreach (var tenant in byTenant)
        {
            await audit.RecordAsync(new AuditRecord(action, AuditTargets.User, user.Id, name, tenant.Key, details(tenant.ToArray())), cancellationToken);
        }
    }
}
