using NLog;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Users;

namespace PSAAS.Application.Services.Users;

/// <summary>UC7: global search on first name, last name and email; tenants are derived from the profiles.</summary>
public sealed class UserSearchService(IUserDirectory directory, IProfileReadRepository profiles)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    public async Task<IReadOnlyList<UserReadModel>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        var users = await directory.SearchAsync(search, cancellationToken);
        var existingProfiles = (await profiles.GetWithTenantAsync(
                users.SelectMany(user => user.ProfileIds).ToArray(), cancellationToken))
            .ToDictionary(profile => profile.Id, StringComparer.Ordinal);

        Logger.Info("Found {UserCount} users for search {Search}", users.Count, search);
        return users.Select(user => UserReadModel.From(user, existingProfiles)).ToArray();
    }
}
