namespace PSAAS.Auth;

public static class KeycloakDefaults
{
    public const string ConfigurationSection = KeycloakOptions.DefaultSectionName;
    public const string ClientCredentialsHttpClientName = "PSAAS.Auth.ClientCredentials";
    public const string TokenHttpClientName = "PSAAS.Auth.Token";
}
