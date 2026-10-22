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
/// The realm user profile must declare the attributes birthDate and profileIds (or allow unmanaged
/// attributes): since Keycloak 24 undeclared attributes are silently dropped.
/// </remarks>
public sealed class KeycloakUserDirectory(HttpClient http) : IUserDirectory
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string BirthDateAttribute = "birthDate";
    private const string ProfileIdsAttribute = "profileIds";
    private const string BirthDateFormat = "yyyy-MM-dd";

    // shortcut: UC7 has no paging, so a search returns at most this many users; add paging when realms grow.
    private const int MaxSearchResults = 100;

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
            Attributes = new Dictionary<string, string[]>
            {
                [BirthDateAttribute] = [user.BirthDate.ToString(BirthDateFormat, CultureInfo.InvariantCulture)],
                [ProfileIdsAttribute] = user.ProfileIds.ToArray()
            }
        };

        var id = await CallAsync(async () =>
        {
            using var response = await http.PostAsJsonAsync("users", representation, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new UserEmailConflictException($"Keycloak already has a user with email {user.Email}");
            }

            EnsureSuccess(response, "create user");
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
                EnsureSuccess(response, "send the password setup email");
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
        return new DirectoryUser(id, user.FirstName, user.LastName, user.Email, user.BirthDate, user.ProfileIds);
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
            EnsureSuccess(response, "search users");
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
                EnsureSuccess(response, "count users");
                return await response.Content.ReadFromJsonAsync<int>(cancellationToken);
            }, cancellationToken);
        }

        return counts;
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
                EnsureSuccess(response, "find the users of a profile");
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
                    EnsureSuccess(response, "update the profiles of a user");
                    return true;
                }, cancellationToken);
            }

            Logger.Info("Removed profile {ProfileId} from {UserCount} Keycloak users", profileId, users.Length);
        }
    }

    private static string ProfileQuery(string profileId) => Uri.EscapeDataString($"{ProfileIdsAttribute}:{profileId}");

    private static DirectoryUser ToDirectoryUser(UserRepresentation user)
    {
        var attributes = user.Attributes ?? [];
        var birthDate = attributes.TryGetValue(BirthDateAttribute, out var values)
            && DateOnly.TryParseExact(values.FirstOrDefault(), BirthDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? parsed
                : (DateOnly?)null;
        return new DirectoryUser(
            user.Id ?? string.Empty,
            user.FirstName ?? string.Empty,
            user.LastName ?? string.Empty,
            user.Email ?? user.Username ?? string.Empty,
            birthDate,
            attributes.TryGetValue(ProfileIdsAttribute, out var profileIds) ? profileIds : []);
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

    private static void EnsureSuccess(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

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
            EnsureSuccess(response, "delete user");
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
        public Dictionary<string, string[]>? Attributes { get; set; }
    }
}
