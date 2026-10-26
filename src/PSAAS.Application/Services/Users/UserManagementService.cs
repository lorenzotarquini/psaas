using NLog;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Application.Contracts.Users;

namespace PSAAS.Application.Services.Users;

/// <summary>UC19 edit body. BirthDate is "YYYY-MM-DD" (a string, for a field code instead of a binding error).</summary>
public sealed record UserUpdateRequest(string? FirstName, string? LastName, string? BirthDate, IReadOnlyList<string>? ProfileIds, bool MfaRequired);

/// <param name="Conflict">The action is not allowed in the current status (UC19 4a).</param>
public sealed record UserActionResult(UserReadModel? User, bool Found, bool Conflict, IReadOnlyDictionary<string, string> Errors)
{
    public static readonly UserActionResult NotFound = new(null, false, false, new Dictionary<string, string>());

    public static readonly UserActionResult NotAllowed = new(null, true, true, new Dictionary<string, string>());

    public bool Succeeded => User is not null;
}

/// <summary>UC19: one user in the side panel of mock 1f, and what the administrator can do with it.</summary>
public sealed class UserManagementService(IUserDirectory directory, IProfileReadRepository profiles, UserRules rules, TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    public async Task<UserReadModel?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await directory.GetAsync(id, cancellationToken);
        return user is null ? null : await ReadAsync(user, cancellationToken);
    }

    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    public async Task<UserActionResult> UpdateAsync(string id, UserUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var current = await GetAsync(id, cancellationToken);
        if (current is null) return UserActionResult.NotFound;

        var errors = new Dictionary<string, string>();
        var firstName = UserRules.Trim(request.FirstName);
        var lastName = UserRules.Trim(request.LastName);
        UserRules.Required(errors, "firstName", firstName);
        UserRules.Required(errors, "lastName", lastName);
        var birthDate = UserRules.ValidateBirthDate(request.BirthDate, errors);
        var profileIds = (request.ProfileIds ?? []).Select(UserRules.Trim).ToArray();
        // The tenants the user already belongs to do not count against their plan limit again.
        var memberOf = current.Profiles.Select(p => p.TenantId).ToHashSet();
        await rules.ValidateProfilesAsync(profileIds, errors, memberOf, cancellationToken);
        if (errors.Count > 0)
        {
            Logger.Info("Update of user {UserId} rejected: {ValidationErrors}", id, errors);
            return new UserActionResult(null, true, false, errors);
        }

        var update = new DirectoryUserUpdate(firstName, lastName, birthDate!.Value, profileIds.Distinct(StringComparer.Ordinal).ToArray(), request.MfaRequired);
        if (!await directory.UpdateAsync(id, update, cancellationToken)) return UserActionResult.NotFound;

        var updated = (await GetAsync(id, cancellationToken))!;
        var changes = Changes(current, updated);
        if (changes.Count > 0)
        {
            // Recorded in the history of the tenants before and after: a removed profile matters to its tenant too.
            var touched = updated with { Profiles = current.Profiles.Concat(updated.Profiles).DistinctBy(p => p.Id).ToArray() };
            await rules.RecordAsync(AuditActions.UserUpdated, touched, _ => new { email = updated.Email, changes }, cancellationToken);
        }

        Logger.Info("Updated user {UserId}: {ChangedFields}", id, changes.Keys);
        return new UserActionResult(updated, true, false, errors);
    }

    /// <summary>UC29-R2: invitations still open after this many days.</summary>
    public const int StaleInviteDays = 5;

    /// <summary>UC29-R2: users whose invitation is open since more than <see cref="StaleInviteDays"/> days.</summary>
    public static IReadOnlyList<DirectoryUser> StaleInvites(IEnumerable<DirectoryUser> users, DateTimeOffset now) =>
        users.Where(u => u.Enabled && u.PasswordPending && (u.LastInviteAt ?? u.CreatedAt) is { } at && at <= now.AddDays(-StaleInviteDays)).ToArray();

    /// <summary>UC29 "Reinvia inviti": the invitation again to every stale one; returns how many.</summary>
    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    public async Task<int> ResendStaleInvitesAsync(CancellationToken cancellationToken = default)
    {
        var stale = StaleInvites(await directory.SearchAsync(null, cancellationToken), clock.GetUtcNow());
        foreach (var user in stale)
        {
            await InviteAsync(user.Id, cancellationToken);
        }

        Logger.Info("Resent {Count} stale invitations", stale.Count);
        return stale.Count;
    }

    /// <summary>UC19-R3: only while the invitation is open.</summary>
    public Task<UserActionResult> InviteAsync(string id, CancellationToken cancellationToken = default) =>
        ActAsync(id, user => user.Status == UserStatuses.Invited, AuditActions.UserInvited,
            () => directory.SendInviteAsync(id, clock.GetUtcNow(), cancellationToken), cancellationToken);

    /// <summary>UC19-R3: for users who could log in (not disabled, not still invited).</summary>
    public Task<UserActionResult> ResetPasswordAsync(string id, CancellationToken cancellationToken = default) =>
        ActAsync(id, user => user.Status is UserStatuses.Active or UserStatuses.Suspended, AuditActions.UserPasswordReset,
            () => directory.SendPasswordResetAsync(id, cancellationToken), cancellationToken);

    public Task<UserActionResult> SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default) =>
        ActAsync(id, user => (user.Status == UserStatuses.Disabled) == enabled, enabled ? AuditActions.UserEnabled : AuditActions.UserDisabled,
            () => directory.SetEnabledAsync(id, enabled, cancellationToken), cancellationToken);

    private async Task<UserActionResult> ActAsync(
        string id,
        Func<UserReadModel, bool> allowed,
        string action,
        Func<Task> act,
        CancellationToken cancellationToken)
    {
        var user = await GetAsync(id, cancellationToken);
        if (user is null) return UserActionResult.NotFound;
        if (!allowed(user))
        {
            Logger.Info("{Action} not allowed for user {UserId} in status {Status}", action, id, user.Status);
            return UserActionResult.NotAllowed;
        }

        await act();
        await rules.RecordAsync(action, user, _ => new { email = user.Email }, cancellationToken);
        Logger.Info("{Action} done for user {UserId}", action, id);
        return new UserActionResult((await GetAsync(id, cancellationToken))!, true, false, new Dictionary<string, string>());
    }

    private async Task<UserReadModel> ReadAsync(DirectoryUser user, CancellationToken cancellationToken)
    {
        var existing = (await profiles.GetWithTenantAsync(user.ProfileIds, cancellationToken)).ToDictionary(p => p.Id, StringComparer.Ordinal);
        var lastLogins = await directory.GetLastLoginsAsync([user.Id], cancellationToken);
        return UserReadModel.From(user, existing, lastLogins.TryGetValue(user.Id, out var at) ? at : null);
    }

    // UC12-style before → after, names of profiles as the history shows them.
    private static Dictionary<string, object> Changes(UserReadModel before, UserReadModel after)
    {
        var changes = new Dictionary<string, object>();
        void Compare(string field, object? a, object? b)
        {
            if (!Equals(a, b)) changes[field] = new { before = a, after = b };
        }

        Compare("firstName", before.FirstName, after.FirstName);
        Compare("lastName", before.LastName, after.LastName);
        Compare("birthDate", before.BirthDate?.ToString("yyyy-MM-dd"), after.BirthDate?.ToString("yyyy-MM-dd"));
        Compare("mfaRequired", before.MfaRequired, after.MfaRequired);
        string Label(UserProfileReadModel p) => $"{p.Name} · {p.TenantCompanyName}";
        var added = after.Profiles.ExceptBy(before.Profiles.Select(p => p.Id), p => p.Id).Select(Label).ToArray();
        var removed = before.Profiles.ExceptBy(after.Profiles.Select(p => p.Id), p => p.Id).Select(Label).ToArray();
        if (added.Length > 0) changes["profilesAdded"] = added;
        if (removed.Length > 0) changes["profilesRemoved"] = removed;
        return changes;
    }
}
