using PSAAS.Application.Abstractions.Geography;
using PSAAS.Application.Abstractions.Users;

namespace PSAAS.AdminPortal.Backend.Tests.Infrastruttura;

/// <summary>Plays Keycloak as the store of PSAAS users.</summary>
public sealed class FakeKeycloakUsers : IUserDirectory
{
    private int _nextId;

    public List<DirectoryUser> Users { get; } = [];

    /// <summary>Emails Keycloak was asked to send the password setup link to.</summary>
    public List<string> PasswordSetupEmails { get; } = [];

    public bool Unavailable { get; set; }

    public DirectoryUser Add(string firstName, string lastName, string email, params string[] profileIds)
    {
        var user = new DirectoryUser($"kc-user-{++_nextId}", firstName, lastName, email, new DateOnly(1980, 1, 1), profileIds);
        Users.Add(user);
        return user;
    }

    public Task<DirectoryUser> CreateAsync(NewDirectoryUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        if (Users.Any(existing => existing.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
        {
            throw new UserEmailConflictException($"User with email {user.Email} exists");
        }

        var created = new DirectoryUser($"kc-user-{++_nextId}", user.FirstName, user.LastName, user.Email, user.BirthDate, user.ProfileIds);
        Users.Add(created);
        PasswordSetupEmails.Add(user.Email);
        return Task.FromResult(created);
    }

    public Task<IReadOnlyList<DirectoryUser>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        var term = search?.Trim() ?? string.Empty;
        return Task.FromResult<IReadOnlyList<DirectoryUser>>(Users
            .Where(user => new[] { user.FirstName, user.LastName, user.Email }
                .Any(value => value.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .ToArray());
    }

    private void ThrowIfUnavailable()
    {
        if (Unavailable)
        {
            throw new UserDirectoryUnavailableException("Keycloak is not reachable");
        }
    }
}

/// <summary>Plays the Italian city list with a fixed set of towns.</summary>
public sealed class FakeItalianCityList : IItalianCityProvider
{
    public List<ItalianCity> Cities { get; } =
    [
        new("Roma", "RM", ["00118", "00100"]),
        new("Milano", "MI", ["20124", "20100"])
    ];

    public bool Unavailable { get; set; }

    public Task<IReadOnlyList<ItalianCity>?> SearchAsync(string term, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ItalianCity>?>(Unavailable
            ? null
            : Cities.Where(city => city.Name.Contains(term.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray());
}
