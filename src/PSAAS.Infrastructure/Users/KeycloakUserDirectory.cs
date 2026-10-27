using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NLog;
using PSAAS.Application.Abstractions.Users;

namespace PSAAS.Infrastructure.Users;

/// <summary>
/// PSAAS users in the Keycloak admin REST API. <paramref name="http"/> has the realm admin URL
/// (e.g. https://kc/admin/realms/PSAAS/) as base address and sends the service account token.
/// </summary>
/// <remarks>
/// The realm user profile must declare the attributes birthDate, profileIds, mfaRequired, createdBy, invitesSent and
/// lastInviteAt (or allow unmanaged attributes): since Keycloak 24 undeclared attributes are silently dropped.
/// Last logins need user events (LOGIN) saved in the realm and the view-events role.
/// </remarks>
public sealed class KeycloakUserDirectory(HttpClient http) : IUserDirectory
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string BirthDateAttribute = "birthDate";
    private const string ProfileIdsAttribute = "profileIds";
    private const string BirthDateFormat = "yyyy-MM-dd";
    private const string MfaRequiredAttribute = "mfaRequired";
    private const string CreatedByAttribute = "createdBy";
    private const string InvitesSentAttribute = "invitesSent";
    private const string LastInviteAtAttribute = "lastInviteAt";
    private const string UpdatePasswordAction = "UPDATE_PASSWORD";
    private const string ConfigureTotpAction = "CONFIGURE_TOTP";

    // shortcut: UC7 filters and pages in memory over at most this many users; move paging into Keycloak when realms grow.
    private const int MaxSearchResults = 1000;

    // shortcut: users of one profile updated in a single page; page through them if a profile ever has more.
    private const int MaxProfileUsers = 1000;

    private static readonly string[] PasswordSetupActions = ["UPDATE_PASSWORD"];

    public async Task<DirectoryUser> CreateAsync(NewDirectoryUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        var representation = new UserRepresentation
        {
            Username = user.Email,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Enabled = true,
            // UC19-R1: the open action marks the invitation as pending until the password is set.
            RequiredActions = [UpdatePasswordAction],
            Attributes = new Dictionary<string, string[]>
            {
                [BirthDateAttribute] = [user.BirthDate.ToString(BirthDateFormat, CultureInfo.InvariantCulture)],
                [ProfileIdsAttribute] = user.ProfileIds.ToArray(),
                [CreatedByAttribute] = user.CreatedBy is null ? [] : [user.CreatedBy],
                [InvitesSentAttribute] = ["1"],
                [LastInviteAtAttribute] = [(user.InvitedAt ?? DateTimeOffset.UtcNow).ToString("O", CultureInfo.InvariantCulture)]
            }
        };

        var id = await CallAsync(async () =>
        {
            using var response = await http.PostAsJsonAsync("users", representation, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new UserEmailConflictException($"Keycloak already has a user with email {user.Email}");
            }

            await EnsureSuccessAsync(response, "create user");
            return response.Headers.Location?.Segments[^1]
                ?? throw new InvalidOperationException("Keycloak created a user without returning its location");
        }, cancellationToken);

        // R6: Keycloak emails the link to set the password, which never goes through PSAAS.
        try
        {
            await CallAsync(async () =>
            {
                using var response = await http.PutAsJsonAsync(
                    $"users/{Uri.EscapeDataString(id)}/execute-actions-email", PasswordSetupActions, cancellationToken);
                await EnsureSuccessAsync(response, "send the password setup email");
                return true;
            }, cancellationToken);
        }
        catch (UserDirectoryUnavailableException exception)
        {
            // UC6 3d: nothing is created, otherwise the user would exist with no way to set a password.
            await DeleteQuietlyAsync(id);
            throw new UserDirectoryUnavailableException("Keycloak could not send the password setup email", exception);
        }

        Logger.Info("Created Keycloak user {KeycloakUserId} and sent the password setup email", id);
        return new DirectoryUser(
            id, user.FirstName, user.LastName, user.Email, user.BirthDate, user.ProfileIds,
            PasswordPending: true, CreatedAt: user.InvitedAt, CreatedBy: user.CreatedBy, InvitesSent: 1, LastInviteAt: user.InvitedAt);
    }

    public async Task<IReadOnlyList<DirectoryUser>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        var term = search?.Trim() ?? string.Empty;
        // briefRepresentation=false returns the attributes; *term* makes Keycloak search infix instead of prefix.
        var url = $"users?briefRepresentation=false&max={MaxSearchResults}"
            + (term.Length == 0 ? string.Empty : $"&search={Uri.EscapeDataString($"*{term}*")}");

        var users = await CallAsync(async () =>
        {
            using var response = await http.GetAsync(url, cancellationToken);
            await EnsureSuccessAsync(response, "search users");
            return await response.Content.ReadFromJsonAsync<UserRepresentation[]>(cancellationToken) ?? [];
        }, cancellationToken);

        Logger.Debug("Keycloak returned {UserCount} users", users.Length);
        return users.Select(ToDirectoryUser).ToArray();
    }

    public async Task<IReadOnlyDictionary<string, int>> CountByProfilesAsync(
        IReadOnlyCollection<string> profileIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profileIds);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        // shortcut: one count request per profile; fine for the tens of profiles of a tenant, batch it if that grows.
        foreach (var profileId in profileIds.Distinct(StringComparer.Ordinal))
        {
            counts[profileId] = await CallAsync(async () =>
            {
                using var response = await http.GetAsync($"users/count?q={ProfileQuery(profileId)}", cancellationToken);
                await EnsureSuccessAsync(response, "count users");
                return await response.Content.ReadFromJsonAsync<int>(cancellationToken);
            }, cancellationToken);
        }

        return counts;
    }

    public async Task<int> CountUsersWithAnyAsync(IReadOnlyCollection<string> profileIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profileIds);
        // Keycloak has no OR on attributes: one search per profile, users counted once.
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profileId in profileIds.Distinct(StringComparer.Ordinal))
        {
            var users = await CallAsync(async () =>
            {
                using var response = await http.GetAsync(
                    $"users?briefRepresentation=true&max={MaxProfileUsers}&q={ProfileQuery(profileId)}", cancellationToken);
                await EnsureSuccessAsync(response, "find the users of a profile");
                return await response.Content.ReadFromJsonAsync<UserRepresentation[]>(cancellationToken) ?? [];
            }, cancellationToken);
            ids.UnionWith(users.Select(user => user.Id!));
        }

        return ids.Count;
    }

    public async Task RemoveProfilesAsync(IReadOnlyCollection<string> profileIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profileIds);
        var removed = profileIds.ToHashSet(StringComparer.Ordinal);
        foreach (var profileId in removed)
        {
            var users = await CallAsync(async () =>
            {
                using var response = await http.GetAsync(
                    $"users?briefRepresentation=false&max={MaxProfileUsers}&q={ProfileQuery(profileId)}", cancellationToken);
                await EnsureSuccessAsync(response, "find the users of a profile");
                return await response.Content.ReadFromJsonAsync<UserRepresentation[]>(cancellationToken) ?? [];
            }, cancellationToken);

            foreach (var user in users)
            {
                // The full representation goes back: Keycloak replaces the attribute map on update.
                var attributes = user.Attributes ?? [];
                attributes[ProfileIdsAttribute] = attributes.GetValueOrDefault(ProfileIdsAttribute, [])
                    .Where(id => !removed.Contains(id)).ToArray();
                user.Attributes = attributes;
                await CallAsync(async () =>
                {
                    using var response = await http.PutAsJsonAsync($"users/{Uri.EscapeDataString(user.Id!)}", user, cancellationToken);
                    await EnsureSuccessAsync(response, "update the profiles of a user");
                    return true;
                }, cancellationToken);
            }

            Logger.Info("Removed profile {ProfileId} from {UserCount} Keycloak users", profileId, users.Length);
        }
    }

    public async Task<DirectoryUser?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(id, cancellationToken);
        return user is null ? null : ToDirectoryUser(user);
    }

    public async Task<bool> UpdateAsync(string id, DirectoryUserUpdate update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        var user = await FindAsync(id, cancellationToken);
        if (user is null) return false;

        user.FirstName = update.FirstName;
        user.LastName = update.LastName;
        var attributes = user.Attributes ?? [];
        attributes[BirthDateAttribute] = [update.BirthDate.ToString(BirthDateFormat, CultureInfo.InvariantCulture)];
        attributes[ProfileIdsAttribute] = update.ProfileIds.ToArray();
        attributes[MfaRequiredAttribute] = [update.MfaRequired ? "true" : "false"];
        user.Attributes = attributes;
        // UC19-R4: Keycloak asks for the OTP setup at the next login while MFA is required.
        var actions = (user.RequiredActions ?? []).Where(action => action != ConfigureTotpAction).ToList();
        if (update.MfaRequired) actions.Add(ConfigureTotpAction);
        user.RequiredActions = actions.ToArray();
        await PutAsync(user, "update a user", cancellationToken);
        Logger.Info("Updated Keycloak user {KeycloakUserId}", id);
        return true;
    }

    public async Task SendInviteAsync(string id, DateTimeOffset sentAt, CancellationToken cancellationToken = default)
    {
        await ExecuteActionsEmailAsync(id, cancellationToken);
        var user = await FindAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Keycloak user {id} disappeared while inviting it");
        var attributes = user.Attributes ?? [];
        var sent = int.TryParse(attributes.GetValueOrDefault(InvitesSentAttribute)?.FirstOrDefault(), CultureInfo.InvariantCulture, out var count) ? count : 1;
        attributes[InvitesSentAttribute] = [(sent + 1).ToString(CultureInfo.InvariantCulture)];
        attributes[LastInviteAtAttribute] = [sentAt.ToString("O", CultureInfo.InvariantCulture)];
        user.Attributes = attributes;
        await PutAsync(user, "count the invitations", cancellationToken);
    }

    public Task SendPasswordResetAsync(string id, CancellationToken cancellationToken = default) =>
        ExecuteActionsEmailAsync(id, cancellationToken);

    public async Task SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Keycloak user {id} disappeared while changing it");
        user.Enabled = enabled;
        await PutAsync(user, enabled ? "enable a user" : "disable a user", cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, DateTimeOffset>> GetLastLoginsAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var result = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        // shortcut: one events request per user of the page (at most 200); use the admin events export if pages grow.
        foreach (var id in userIds.Distinct(StringComparer.Ordinal))
        {
            try
            {
                using var response = await http.GetAsync($"events?type=LOGIN&user={Uri.EscapeDataString(id)}&max=1", cancellationToken);
                response.EnsureSuccessStatusCode();
                var events = await response.Content.ReadFromJsonAsync<EventRepresentation[]>(cancellationToken) ?? [];
                if (events.Length > 0) result[id] = DateTimeOffset.FromUnixTimeMilliseconds(events[0].Time);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                // UC19-R5: events off or no view-events role: no last login, nothing else breaks.
                Logger.Warn(exception, "Keycloak login events unavailable");
                break;
            }
        }

        return result;
    }

    private async Task<UserRepresentation?> FindAsync(string id, CancellationToken cancellationToken) =>
        await CallAsync(async () =>
        {
            using var response = await http.GetAsync($"users/{Uri.EscapeDataString(id)}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            await EnsureSuccessAsync(response, "read a user");
            return await response.Content.ReadFromJsonAsync<UserRepresentation>(cancellationToken);
        }, cancellationToken);

    // The full representation goes back: Keycloak replaces the attribute map on update.
    private Task PutAsync(UserRepresentation user, string operation, CancellationToken cancellationToken) =>
        CallAsync(async () =>
        {
            using var response = await http.PutAsJsonAsync($"users/{Uri.EscapeDataString(user.Id!)}", user, cancellationToken);
            await EnsureSuccessAsync(response, operation);
            return true;
        }, cancellationToken);

    // UC19-R3: Keycloak emails the link; the password never goes through PSAAS.
    private Task ExecuteActionsEmailAsync(string id, CancellationToken cancellationToken) =>
        CallAsync(async () =>
        {
            using var response = await http.PutAsJsonAsync(
                $"users/{Uri.EscapeDataString(id)}/execute-actions-email", PasswordSetupActions, cancellationToken);
            await EnsureSuccessAsync(response, "send the password email");
            return true;
        }, cancellationToken);

    private static string ProfileQuery(string profileId) => Uri.EscapeDataString($"{ProfileIdsAttribute}:{profileId}");

    private static DirectoryUser ToDirectoryUser(UserRepresentation user)
    {
        var attributes = user.Attributes ?? [];
        var birthDate = attributes.TryGetValue(BirthDateAttribute, out var values)
            && DateOnly.TryParseExact(values.FirstOrDefault(), BirthDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? parsed
                : (DateOnly?)null;
        string? First(string name) => attributes.TryGetValue(name, out var v) ? v.FirstOrDefault() : null;
        return new DirectoryUser(
            user.Id ?? string.Empty,
            user.FirstName ?? string.Empty,
            user.LastName ?? string.Empty,
            user.Email ?? user.Username ?? string.Empty,
            birthDate,
            attributes.TryGetValue(ProfileIdsAttribute, out var profileIds) ? profileIds : [],
            user.Enabled,
            (user.RequiredActions ?? []).Contains(UpdatePasswordAction),
            First(MfaRequiredAttribute) == "true",
            user.CreatedTimestamp is { } created ? DateTimeOffset.FromUnixTimeMilliseconds(created) : null,
            First(CreatedByAttribute),
            int.TryParse(First(InvitesSentAttribute), CultureInfo.InvariantCulture, out var invites) ? invites : 0,
            DateTimeOffset.TryParse(First(LastInviteAtAttribute), CultureInfo.InvariantCulture, DateTimeStyles.None, out var invitedAt) ? invitedAt : null);
    }

    /// <summary>
    /// Turns every way the user directory can fail into one exception (503 for the portal): Keycloak down
    /// (network, timeout, 5xx, token endpoint) or misconfigured (no client secret, missing service account roles).
    /// </summary>
    private static async Task<T> CallAsync<T>(Func<Task<T>> call, CancellationToken cancellationToken)
    {
        try
        {
            return await call();
        }
        catch (Exception exception) when (
            exception is HttpRequestException or JsonException
            || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            Logger.Warn(exception, "Keycloak admin API unavailable");
            throw new UserDirectoryUnavailableException("Keycloak is not reachable", exception);
        }
        catch (InvalidOperationException exception)
        {
            // Retrying will not help: Keycloak:ClientSecret or the realm-management roles must be fixed.
            Logger.Error(exception, "Keycloak admin API misconfigured");
            throw new UserDirectoryUnavailableException("Keycloak admin API is misconfigured", exception);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // Keycloak says why in the body ({"errorMessage":"Failed to send execute actions email"}): no secrets in it.
        var reason = await response.Content.ReadAsStringAsync();
        Logger.Error("Keycloak could not {Operation}: HTTP {StatusCode} {Reason}", operation, (int)response.StatusCode, reason);

        if ((int)response.StatusCode >= 500)
        {
            throw new HttpRequestException($"Keycloak could not {operation}", null, response.StatusCode);
        }

        // 401/403/400: the service account or the realm is misconfigured.
        throw new InvalidOperationException($"Keycloak refused to {operation} (HTTP {(int)response.StatusCode})");
    }

    private async Task DeleteQuietlyAsync(string id)
    {
        try
        {
            using var response = await http.DeleteAsync($"users/{Uri.EscapeDataString(id)}", CancellationToken.None);
            await EnsureSuccessAsync(response, "delete user");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, "Keycloak user {KeycloakUserId} was created but could not be removed after the email failure", id);
        }
    }

    private sealed class UserRepresentation
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool Enabled { get; set; }
        public long? CreatedTimestamp { get; set; }
        public string[]? RequiredActions { get; set; }
        public Dictionary<string, string[]>? Attributes { get; set; }
    }

    private sealed class EventRepresentation
    {
        public long Time { get; set; }
    }
}
