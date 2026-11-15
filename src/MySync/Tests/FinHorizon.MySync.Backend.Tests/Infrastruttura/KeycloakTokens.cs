using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FinHorizon.MySync.Backend.Tests.Infrastruttura;

/// <summary>
/// Plays Keycloak as token issuer: same issuer, audience and claim shape as the PSAAS realm,
/// signed with a local key that the backend under test trusts.
/// </summary>
public static class KeycloakTokens
{
    public const string Issuer = "https://auth.fin-horizon.com/realms/PSAAS";
    public const string Audience = "mysync-api";

    /// <summary>The realm role of MySync users (group end_users).</summary>
    public const string EndUserRole = "enduser";

    public static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("fnhz-mysync-tests-signing-key-0123456789"));

    /// <summary>A MySync user: "Mario" becomes subject "kc-mario", email "mario@example.com".</summary>
    public static string EndUser(string name) => Create(name, [EndUserRole]);

    public static string Create(string name, string[] roles, string audience = Audience)
    {
        var user = name.ToLowerInvariant();
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity([new Claim("sub", $"kc-{user}")]),
            Claims = new Dictionary<string, object>
            {
                ["preferred_username"] = user,
                ["email"] = $"{user}@example.com",
                ["realm_access"] = new Dictionary<string, object> { ["roles"] = roles }
            },
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        });
    }
}
