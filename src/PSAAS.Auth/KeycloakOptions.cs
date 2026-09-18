using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace PSAAS.Auth;

public sealed class KeycloakOptions
{
    public const string DefaultSectionName = "Keycloak";

    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string? ClientSecret { get; set; }

    public KeycloakAuthenticationMode Mode { get; set; } = KeycloakAuthenticationMode.Api;

    public bool RequireHttpsMetadata { get; set; } = true;

    public string RoleClaimType { get; set; } = System.Security.Claims.ClaimTypes.Role;

    public string NameClaimType { get; set; } = "preferred_username";

    public bool ValidateAudience { get; set; }

    public string[] ValidAudiences { get; set; } = [];

    public string[] Scopes { get; set; } = ["openid", "profile", "email"];

    public string CallbackPath { get; set; } = "/signin-oidc";

    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";

    public string RemoteSignOutPath { get; set; } = "/signout-oidc";

    public bool SaveTokens { get; set; } = true;

    public string CookieScheme { get; set; } = "Cookies";

    public string OpenIdConnectScheme { get; set; } = "OpenIdConnect";

    public string JwtBearerScheme { get; set; } = "Bearer";

    public bool IncludeResourceAccessRoles { get; set; } = true;

    public bool IncludeRealmAccessRoles { get; set; } = true;

    public string[] RoleResourceClientIds { get; set; } = [];

    public IList<KeycloakPolicyOptions> Policies { get; } = new List<KeycloakPolicyOptions>();

    public KeycloakClientCredentialsOptions ClientCredentials { get; set; } = new();

    public Action<TokenValidatedContext>? OnOpenIdConnectTokenValidated { get; set; }

    public Action<Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext>? OnJwtBearerTokenValidated { get; set; }
}
