using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FinHorizon.Auth.Internal;
using Xunit;

namespace FinHorizon.Auth.Tests;

public sealed class KeycloakRoleClaimsTests
{
    [Fact]
    public void AddRoleClaims_AddsRealmAndResourceRoles()
    {
        var token = new JwtSecurityToken(
            claims: [new Claim("sub", "user-1")],
            signingCredentials: null);
        token.Payload["realm_access"] = new Dictionary<string, object>
        {
            ["roles"] = new[] { "SystemAdmin" }
        };
        token.Payload["resource_access"] = new Dictionary<string, object>
        {
            ["fnhz-api"] = new Dictionary<string, object>
            {
                ["roles"] = new[] { "Reader", "Writer" }
            }
        };

        var identity = new ClaimsIdentity();

        KeycloakRoleClaims.AddRoleClaims(identity, token, new KeycloakOptions
        {
            RoleResourceClientIds = ["fnhz-api"]
        });

        Assert.Contains(identity.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "SystemAdmin");
        Assert.Contains(identity.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Reader");
        Assert.Contains(identity.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Writer");
    }
}
