using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace PSAAS.Auth.Internal;

internal static class KeycloakRoleClaims
{
    public static void AddRoleClaims(ClaimsIdentity identity, JwtSecurityToken token, KeycloakOptions options)
    {
        if (options.IncludeRealmAccessRoles)
        {
            foreach (var role in GetRealmRoles(token))
            {
                AddClaimIfMissing(identity, options.RoleClaimType, role);
            }
        }

        if (options.IncludeResourceAccessRoles)
        {
            foreach (var role in GetResourceRoles(token, options.RoleResourceClientIds))
            {
                AddClaimIfMissing(identity, options.RoleClaimType, role);
            }
        }
    }

    public static IEnumerable<string> GetRealmRoles(JwtSecurityToken token)
    {
        if (!token.Payload.TryGetValue("realm_access", out var value))
        {
            yield break;
        }

        foreach (var role in ReadRoles(value))
        {
            yield return role;
        }
    }

    public static IEnumerable<string> GetResourceRoles(JwtSecurityToken token, IEnumerable<string> clientIds)
    {
        if (!token.Payload.TryGetValue("resource_access", out var value))
        {
            yield break;
        }

        var allowedClients = new HashSet<string>(clientIds ?? [], StringComparer.OrdinalIgnoreCase);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var client in document.RootElement.EnumerateObject())
        {
            if (allowedClients.Count > 0 && !allowedClients.Contains(client.Name))
            {
                continue;
            }

            foreach (var role in ReadRoles(client.Value))
            {
                yield return role;
            }
        }
    }

    private static IEnumerable<string> ReadRoles(object? value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        return ReadRoles(document.RootElement).ToArray();
    }

    private static IEnumerable<string> ReadRoles(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
        {
            foreach (var role in roles.EnumerateArray())
            {
                if (role.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(role.GetString()))
                {
                    yield return role.GetString()!;
                }
            }
        }
    }

    private static void AddClaimIfMissing(ClaimsIdentity identity, string type, string value)
    {
        if (!identity.HasClaim(type, value))
        {
            identity.AddClaim(new Claim(type, value));
        }
    }
}
