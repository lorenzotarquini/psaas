namespace PSAAS.Auth.Internal;

internal static class KeycloakOptionsValidator
{
    public static void Validate(KeycloakOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Authority))
        {
            throw new InvalidOperationException("Keycloak Authority is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId) && options.Mode != KeycloakAuthenticationMode.MachineToMachine)
        {
            throw new InvalidOperationException("Keycloak ClientId is required.");
        }

        if (options.Mode == KeycloakAuthenticationMode.MachineToMachine)
        {
            ValidateClientCredentials(options);
        }
    }

    public static void ValidateClientCredentials(KeycloakOptions options)
    {
        var clientId = options.ClientCredentials.ClientId ?? options.ClientId;
        var clientSecret = options.ClientCredentials.ClientSecret ?? options.ClientSecret;

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("Keycloak client credentials ClientId is required.");
        }

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("Keycloak client credentials ClientSecret is required.");
        }
    }
}
