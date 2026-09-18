namespace PSAAS.Auth;

public sealed class KeycloakPolicyOptions
{
    public string Name { get; set; } = string.Empty;

    public bool RequireAuthenticatedUser { get; set; } = true;

    public string[] Roles { get; set; } = [];

    public Dictionary<string, string[]> Claims { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string[] AuthenticationSchemes { get; set; } = [];
}
