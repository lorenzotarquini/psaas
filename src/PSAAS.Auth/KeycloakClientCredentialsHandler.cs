using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSAAS.Auth.Internal;

namespace PSAAS.Auth;

public sealed class KeycloakClientCredentialsHandler : DelegatingHandler
{
    private static readonly TimeSpan ExpirationMargin = TimeSpan.FromSeconds(30);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<KeycloakClientCredentialsHandler> _logger;
    private readonly KeycloakOptions _options;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public KeycloakClientCredentialsHandler(
        IOptions<KeycloakOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<KeycloakClientCredentialsHandler> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt)
        {
            return _cachedToken;
        }

        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt)
            {
                return _cachedToken;
            }

            await AcquireTokenAsync(cancellationToken).ConfigureAwait(false);
            return _cachedToken!;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task AcquireTokenAsync(CancellationToken cancellationToken)
    {
        KeycloakOptionsValidator.ValidateClientCredentials(_options);

        var clientId = _options.ClientCredentials.ClientId ?? _options.ClientId;
        var clientSecret = _options.ClientCredentials.ClientSecret ?? _options.ClientSecret;
        var tokenEndpoint = $"{_options.Authority.TrimEnd('/')}/protocol/openid-connect/token";
        var client = _httpClientFactory.CreateClient(KeycloakDefaults.TokenHttpClientName);
        client.Timeout = TimeSpan.FromSeconds(_options.ClientCredentials.TimeoutSeconds);

        for (var attempt = 0; attempt <= _options.ClientCredentials.RetryCount; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = CreateTokenRequestContent(clientId, clientSecret!)
            };

            try
            {
                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Keycloak token endpoint returned status code {StatusCode}.", (int)response.StatusCode);
                    response.EnsureSuccessStatusCode();
                }

                await CacheTokenAsync(response, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (attempt < _options.ClientCredentials.RetryCount)
            {
                _logger.LogWarning(ex, "Keycloak token acquisition attempt {Attempt} failed. Retrying.", attempt + 1);
            }
        }
    }

    private FormUrlEncodedContent CreateTokenRequestContent(string clientId, string clientSecret)
    {
        var values = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        };

        if (_options.ClientCredentials.Scopes.Length > 0)
        {
            values["scope"] = string.Join(' ', _options.ClientCredentials.Scopes);
        }

        return new FormUrlEncodedContent(values);
    }

    private async Task CacheTokenAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;
        var token = root.GetProperty("access_token").GetString();
        var expiresIn = root.TryGetProperty("expires_in", out var expiresElement) ? expiresElement.GetInt32() : 0;

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Keycloak token endpoint returned an empty access_token.");
        }

        if (expiresIn <= 0)
        {
            throw new InvalidOperationException("Keycloak token endpoint returned an invalid expires_in value.");
        }

        _cachedToken = token;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn).Subtract(ExpirationMargin);
        _logger.LogDebug("Keycloak client credentials token acquired. Expires in {ExpiresInSeconds} seconds.", expiresIn);
    }
}
