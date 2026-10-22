namespace PSAAS.Application.Abstractions.Users;

/// <summary>
/// The identity provider that owns PSAAS users (Keycloak): users are never stored in the PSAAS database.
/// </summary>
public interface IUserDirectory
{
    /// <summary>
    /// Creates the user with the email as username and asks the identity provider to email
    /// a link for setting the password: the password never goes through PSAAS.
    /// </summary>
    /// <exception cref="UserEmailConflictException">The email is already used by another user.</exception>
    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    Task<DirectoryUser> CreateAsync(NewDirectoryUser user, CancellationToken cancellationToken = default);

    /// <summary>Users whose first name, last name or email contains <paramref name="search"/>, ignoring case.</summary>
    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    Task<IReadOnlyList<DirectoryUser>> SearchAsync(string? search, CancellationToken cancellationToken = default);

    /// <summary>How many users have each profile (UC2: "Utenti" column of the organization).</summary>
    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    Task<IReadOnlyDictionary<string, int>> CountByProfilesAsync(
        IReadOnlyCollection<string> profileIds,
        CancellationToken cancellationToken = default);

    /// <summary>EXP-02: how many distinct users have at least one of the profiles (the users of a tenant).</summary>
    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    Task<int> CountUsersWithAnyAsync(IReadOnlyCollection<string> profileIds, CancellationToken cancellationToken = default);

    /// <summary>UC5-R4: removes the given profiles from every user that has them.</summary>
    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    Task RemoveProfilesAsync(IReadOnlyCollection<string> profileIds, CancellationToken cancellationToken = default);

    /// <summary>UC19: one user, or null when it does not exist.</summary>
    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    Task<DirectoryUser?> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>UC19: replaces names, birth date, profiles and the MFA requirement; the email never changes.</summary>
    /// <returns>False when the user does not exist.</returns>
    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    Task<bool> UpdateAsync(string id, DirectoryUserUpdate update, CancellationToken cancellationToken = default);

    /// <summary>UC19-R3: emails the invitation (password setup link) again and counts it.</summary>
    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    Task SendInviteAsync(string id, DateTimeOffset sentAt, CancellationToken cancellationToken = default);

    /// <summary>UC19-R3: emails a link to set a new password.</summary>
    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    Task SendPasswordResetAsync(string id, CancellationToken cancellationToken = default);

    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    Task SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>UC19-R5: last login of each user that has one; never fails (login events may be off).</summary>
    Task<IReadOnlyDictionary<string, DateTimeOffset>> GetLastLoginsAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default);
}

public sealed record NewDirectoryUser(
    string FirstName,
    string LastName,
    string Email,
    DateOnly BirthDate,
    IReadOnlyList<string> ProfileIds,
    string? CreatedBy = null,
    DateTimeOffset? InvitedAt = null);

public sealed record DirectoryUserUpdate(string FirstName, string LastName, DateOnly BirthDate, IReadOnlyList<string> ProfileIds, bool MfaRequired);

/// <param name="BirthDate">Null for users created outside the portal, which may lack the attribute.</param>
/// <param name="ProfileIds">As stored on the user: may still contain profiles deleted since.</param>
/// <param name="PasswordPending">The user has not set a password yet: the invitation is still open.</param>
public sealed record DirectoryUser(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly? BirthDate,
    IReadOnlyList<string> ProfileIds,
    bool Enabled = true,
    bool PasswordPending = false,
    bool MfaRequired = false,
    DateTimeOffset? CreatedAt = null,
    string? CreatedBy = null,
    int InvitesSent = 0,
    DateTimeOffset? LastInviteAt = null);

public sealed class UserEmailConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class UserDirectoryUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
