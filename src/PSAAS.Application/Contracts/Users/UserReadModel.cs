using PSAAS.Application.Abstractions.Users;

namespace PSAAS.Application.Contracts.Users;

/// <summary>A user as the Admin Portal shows it (UC6, UC7, UC19): its profiles come with the tenant they belong to.</summary>
/// <param name="BirthDate">Null for users created outside the portal (e.g. directly in Keycloak).</param>
/// <param name="Status">One of <see cref="UserStatuses"/> (UC19-R1).</param>
public sealed record UserReadModel(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly? BirthDate,
    IReadOnlyList<UserProfileReadModel> Profiles,
    string Status = UserStatuses.Active,
    bool MfaRequired = false,
    DateTimeOffset? LastLoginAt = null,
    DateTimeOffset? CreatedAt = null,
    string? CreatedBy = null,
    int InvitesSent = 0,
    DateTimeOffset? LastInviteAt = null)
{
    /// <summary>Profiles stored on the user that no longer exist (removed with UC5) are left out.</summary>
    public static UserReadModel From(
        DirectoryUser user,
        IReadOnlyDictionary<string, UserProfileReadModel> existingProfiles,
        DateTimeOffset? lastLoginAt = null)
    {
        var profiles = user.ProfileIds
            .Distinct(StringComparer.Ordinal)
            .Where(existingProfiles.ContainsKey)
            .Select(profileId => existingProfiles[profileId])
            .ToArray();
        return new(
            user.Id, user.FirstName, user.LastName, user.Email, user.BirthDate, profiles,
            StatusOf(user, profiles), user.MfaRequired, lastLoginAt, user.CreatedAt, user.CreatedBy, user.InvitesSent, user.LastInviteAt);
    }

    // UC19-R1: disabled wins, then the open invitation; suspended when every tenant of its profiles is.
    private static string StatusOf(DirectoryUser user, UserProfileReadModel[] profiles) =>
        !user.Enabled ? UserStatuses.Disabled
        : user.PasswordPending ? UserStatuses.Invited
        : profiles.Length > 0 && profiles.All(p => p.TenantStatus is "Suspended" or "Ceased") ? UserStatuses.Suspended
        : UserStatuses.Active;
}

/// <param name="TenantStatus">The lifecycle status of the tenant (UC13), for the user status (UC19-R1).</param>
public sealed record UserProfileReadModel(string Id, string Name, string TenantId, string TenantCompanyName, string? TenantStatus = null);

public static class UserStatuses
{
    public const string Invited = "Invited";
    public const string Active = "Active";
    public const string Suspended = "Suspended";
    public const string Disabled = "Disabled";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Invited, Active, Suspended, Disabled };
}
