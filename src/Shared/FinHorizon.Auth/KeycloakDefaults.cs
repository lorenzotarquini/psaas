namespace FinHorizon.Auth;

public static class KeycloakDefaults
{
    public const string ConfigurationSection = KeycloakOptions.DefaultSectionName;
    public const string LogoutPath = "/authentication/logout";
    public const string ClientCredentialsHttpClientName = "FinHorizon.Auth.ClientCredentials";
    public const string TokenHttpClientName = "FinHorizon.Auth.Token";
}
