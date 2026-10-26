using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using NLog;
using PSAAS.Application.Abstractions.Health;
using PSAAS.Application.Abstractions.Impersonation;
using PSAAS.Auth;

namespace PSAAS.AdminPortal.Backend.Infrastructure;

/// <summary>
/// UC31 through Keycloak token exchange: the adminportal-api client, with the impersonation role of realm-management,
/// asks for a token of the user for the platform client (Impersonation:Audience); the platform at
/// Impersonation:PlatformUrl receives it in the fragment (UC31-R2). Token exchange must be enabled in Keycloak.
/// </summary>
public sealed class KeycloakImpersonationGateway(IHttpClientFactory http, IOptions<KeycloakOptions> keycloak, IConfiguration configuration)
    : IImpersonationGateway
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private string? PlatformUrl => configuration["Impersonation:PlatformUrl"];
    private string? Audience => configuration["Impersonation:Audience"];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PlatformUrl) && !string.IsNullOrWhiteSpace(Audience) && !string.IsNullOrWhiteSpace(keycloak.Value.ClientSecret);

    public async Task<string> CreateSessionUrlAsync(string userId, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) throw new ImpersonationUnavailableException("Impersonation is not configured");
        var options = keycloak.Value;
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
            ["client_id"] = options.ClientCredentials.ClientId ?? options.ClientId!,
            ["client_secret"] = options.ClientCredentials.ClientSecret ?? options.ClientSecret!,
            ["requested_subject"] = userId,
            ["audience"] = Audience!,
            ["requested_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
        });
        try
        {
            using var response = await http.CreateClient(nameof(KeycloakImpersonationGateway))
                .PostAsync($"{options.Authority.TrimEnd('/')}/protocol/openid-connect/token", form, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // The body explains why (feature off, missing role…) and holds no token.
                Logger.Error("Keycloak refused the token exchange for user {UserId}: HTTP {Status} {Body}",
                    userId, (int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
                throw new ImpersonationUnavailableException($"Keycloak refused the token exchange (HTTP {(int)response.StatusCode})");
            }

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new ImpersonationUnavailableException("Keycloak answered without a token");
            // The platform reads the token from the fragment, which browsers never send to servers.
            return $"{PlatformUrl!.TrimEnd('/')}/impersonate#access_token={Uri.EscapeDataString(token.AccessToken)}&expires_in={(int)duration.TotalSeconds}";
        }
        catch (HttpRequestException exception)
        {
            throw new ImpersonationUnavailableException("Keycloak is not reachable", exception);
        }
    }

    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
}

/// <summary>UC26: whether impersonation can be used (configuration only).</summary>
public sealed class ImpersonationProbe(IImpersonationGateway gateway) : IServiceProbe
{
    public string Key => "impersonation";

    public Task<ProbeResult> CheckAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new ProbeResult(gateway.IsConfigured ? ServiceStatuses.Up : ServiceStatuses.NotConfigured));
}
