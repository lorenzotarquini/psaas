using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace PSAAS.AdminPortal.Backend.Tests.Infrastruttura;

/// <summary>
/// Plays Keycloak as token issuer: same issuer, audience and claim shape as the PSAAS realm,
/// signed with a local key that the backend under test trusts.
/// </summary>
public static class KeycloakTokens
{
    public const string Issuer = "https://auth.fin-horizon.com/realms/PSAAS";
    public const string Audience = "adminportal-api";
    public const string AdminSubject = "kc-admin";
    public const string AdminUserName = "admin.psaas";
    public const string AdminEmail = "admin@psaas.it";

    public static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("psaas-acceptance-tests-signing-key-0123456789"));

    public static string Administrator() => Create(roles: ["Administrator"]);

    public static string Create(
        string[] roles,
        string audience = Audience,
        bool expired = false,
        string subject = AdminSubject,
        string userName = AdminUserName,
        string email = AdminEmail)
    {
        // Expired well beyond the default 5-minute clock skew.
        var now = expired ? DateTime.UtcNow.AddHours(-2) : DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity([new Claim("sub", subject)]),
            Claims = new Dictionary<string, object>
            {
                ["preferred_username"] = userName,
                ["email"] = email,
                ["realm_access"] = new Dictionary<string, object> { ["roles"] = roles }
            },
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(5),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        });
    }
}
