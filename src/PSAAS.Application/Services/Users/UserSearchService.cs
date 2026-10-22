using NLog;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Users;

namespace PSAAS.Application.Services.Users;

/// <summary>UC7 search filters (mock 1f): text, tenant, profile and status, one page at a time.</summary>
public sealed record UserQuery(string? Search, string? TenantId, string? ProfileId, string? Status, int Page = 1, int PageSize = 50);

/// <summary>UC7: global search on first name, last name and email; tenants are derived from the profiles.</summary>
public sealed class UserSearchService(IUserDirectory directory, IProfileReadRepository profiles)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    public async Task<PagedResult<UserReadModel>> SearchAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var users = await directory.SearchAsync(query.Search, cancellationToken);
        var existingProfiles = (await profiles.GetWithTenantAsync(
                users.SelectMany(user => user.ProfileIds).ToArray(), cancellationToken))
            .ToDictionary(profile => profile.Id, StringComparer.Ordinal);

        // shortcut: filters and paging in memory over what the directory returned (its search cap); move them into
        // Keycloak queries when realms outgrow it.
        var matching = users
            .Select(user => UserReadModel.From(user, existingProfiles))
            .Where(user => query.TenantId is null || user.Profiles.Any(p => p.TenantId == query.TenantId))
            .Where(user => query.ProfileId is null || user.Profiles.Any(p => p.Id == query.ProfileId))
            .Where(user => query.Status is null || user.Status == query.Status)
            .OrderBy(user => user.LastName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(user => user.FirstName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        var page = matching.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToArray();
        var lastLogins = await directory.GetLastLoginsAsync(page.Select(user => user.Id).ToArray(), cancellationToken);

        Logger.Info("Found {UserCount} users for search {Search}", matching.Length, query.Search);
        return new PagedResult<UserReadModel>(
            page.Select(user => user with { LastLoginAt = lastLogins.TryGetValue(user.Id, out var at) ? at : null }).ToArray(),
            matching.Length,
            query.Page,
            query.PageSize);
    }
}
