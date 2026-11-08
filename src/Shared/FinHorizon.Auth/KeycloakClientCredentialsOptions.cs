namespace FinHorizon.Auth;

public sealed class KeycloakClientCredentialsOptions
{
    public string HttpClientName { get; set; } = "FinHorizon.Auth.ClientCredentials";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string[] Scopes { get; set; } = [];

    public int TimeoutSeconds { get; set; } = 10;

    public int RetryCount { get; set; } = 2;
}
