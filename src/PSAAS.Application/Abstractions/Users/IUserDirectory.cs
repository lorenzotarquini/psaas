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
}

public sealed record NewDirectoryUser(
    string FirstName,
    string LastName,
    string Email,
    DateOnly BirthDate,
    IReadOnlyList<string> ProfileIds);

/// <param name="BirthDate">Null for users created outside the portal, which may lack the attribute.</param>
/// <param name="ProfileIds">As stored on the user: may still contain profiles deleted since.</param>
public sealed record DirectoryUser(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly? BirthDate,
    IReadOnlyList<string> ProfileIds);

public sealed class UserEmailConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class UserDirectoryUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
