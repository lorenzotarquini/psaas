using PSAAS.Application.Abstractions.Users;

namespace PSAAS.Application.Contracts.Users;

/// <summary>A user as the Admin Portal shows it (UC6, UC7): its profiles come with the tenant they belong to.</summary>
/// <param name="BirthDate">Null for users created outside the portal (e.g. directly in Keycloak).</param>
public sealed record UserReadModel(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly? BirthDate,
    IReadOnlyList<UserProfileReadModel> Profiles)
{
    /// <summary>Profiles stored on the user that no longer exist (removed with UC5) are left out.</summary>
    public static UserReadModel From(DirectoryUser user, IReadOnlyDictionary<string, UserProfileReadModel> existingProfiles) =>
        new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.BirthDate,
            user.ProfileIds
                .Distinct(StringComparer.Ordinal)
                .Where(existingProfiles.ContainsKey)
                .Select(profileId => existingProfiles[profileId])
                .ToArray());
}

public sealed record UserProfileReadModel(string Id, string Name, string TenantId, string TenantCompanyName);
