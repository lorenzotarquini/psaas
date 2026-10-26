using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Abstractions.Communications;
using PSAAS.Application.Abstractions.Geography;
using PSAAS.Application.Abstractions.Impersonation;
using PSAAS.Application.Contracts.Catalog;
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

    /// <summary>A user who has not set the password yet, invited at the given time.</summary>
    public DirectoryUser AddInvited(string firstName, string lastName, string email, DateTimeOffset invitedAt)
    {
        var user = Add(firstName, lastName, email) with { PasswordPending = true, InvitesSent = 1, CreatedAt = invitedAt, LastInviteAt = invitedAt };
        Users[^1] = user;
        return user;
    }

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

        var created = new DirectoryUser(
            $"kc-user-{++_nextId}", user.FirstName, user.LastName, user.Email, user.BirthDate, user.ProfileIds,
            PasswordPending: true, CreatedAt: user.InvitedAt, CreatedBy: user.CreatedBy, InvitesSent: 1, LastInviteAt: user.InvitedAt);
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

    public Task<IReadOnlyDictionary<string, int>> CountByProfilesAsync(
        IReadOnlyCollection<string> profileIds,
        CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        return Task.FromResult<IReadOnlyDictionary<string, int>>(profileIds.Distinct().ToDictionary(
            id => id,
            id => Users.Count(user => user.ProfileIds.Contains(id))));
    }

    public Task<int> CountUsersWithAnyAsync(IReadOnlyCollection<string> profileIds, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        return Task.FromResult(Users.Count(user => user.ProfileIds.Intersect(profileIds).Any()));
    }

    public Task RemoveProfilesAsync(IReadOnlyCollection<string> profileIds, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        for (var i = 0; i < Users.Count; i++)
        {
            Users[i] = Users[i] with { ProfileIds = Users[i].ProfileIds.Except(profileIds).ToArray() };
        }

        return Task.CompletedTask;
    }


    /// <summary>Emails asked of Keycloak by UC19: (user id, "invite" or "reset").</summary>
    public List<(string UserId, string Kind)> ActionEmails { get; } = [];

    public Dictionary<string, DateTimeOffset> LastLogins { get; } = [];

    public Task<DirectoryUser?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        return Task.FromResult(Users.FirstOrDefault(user => user.Id == id));
    }

    public Task<bool> UpdateAsync(string id, DirectoryUserUpdate update, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        var index = Users.FindIndex(user => user.Id == id);
        if (index < 0) return Task.FromResult(false);
        Users[index] = Users[index] with
        {
            FirstName = update.FirstName,
            LastName = update.LastName,
            BirthDate = update.BirthDate,
            ProfileIds = update.ProfileIds,
            MfaRequired = update.MfaRequired,
        };
        return Task.FromResult(true);
    }

    public Task SendInviteAsync(string id, DateTimeOffset sentAt, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        var index = Users.FindIndex(user => user.Id == id);
        Users[index] = Users[index] with { InvitesSent = Users[index].InvitesSent + 1, LastInviteAt = sentAt };
        ActionEmails.Add((id, "invite"));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string id, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        ActionEmails.Add((id, "reset"));
        return Task.CompletedTask;
    }

    public Task SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        var index = Users.FindIndex(user => user.Id == id);
        Users[index] = Users[index] with { Enabled = enabled };
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, DateTimeOffset>> GetLastLoginsAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<string, DateTimeOffset>>(LastLogins.Where(l => userIds.Contains(l.Key)).ToDictionary());

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

/// <summary>Plays the provisioning hooks of the modules: those in Failing answer with a timeout.</summary>
public sealed class FakeModuleHooks : IModuleProvisioner
{
    public HashSet<string> Failing { get; } = [];

    public Task<ProvisioningOutcome> ProvisionAsync(ModuleReadModel module, Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failing.Contains(module.Id) ? new ProvisioningOutcome(false, "hook provision → timeout") : new ProvisioningOutcome(true, null));
}

/// <summary>Plays the mail server: what was sent, and mailboxes that refuse.</summary>
public sealed class FakeMailbox : IEmailSender
{
    public bool IsConfigured { get; set; } = true;

    public List<EmailMessage> Sent { get; } = [];

    public HashSet<string> Rejecting { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (Rejecting.Contains(message.To)) throw new EmailDeliveryException($"550 mailbox {message.To} unavailable");
        Sent.Add(message);
        return Task.CompletedTask;
    }
}

/// <summary>Plays Keycloak token exchange: a fixed platform address with the user and the duration.</summary>
public sealed class FakeImpersonation : IImpersonationGateway
{
    public bool IsConfigured { get; set; } = true;

    public Task<string> CreateSessionUrlAsync(string userId, TimeSpan duration, CancellationToken cancellationToken = default) =>
        Task.FromResult($"https://platform.test/impersonate#access_token=token-of-{userId}&expires_in={(int)duration.TotalSeconds}");
}
