using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>
/// Hosts the backend with a local signing key in place of Keycloak, so tests can mint their own tokens.
/// </summary>
public sealed class BackendFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "https://auth.fin-horizon.com/realms/PSAAS";
    public const string Audience = "adminportal-api";

    private static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("psaas-backend-tests-signing-key-0123456789"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:psaas-db", "Host=localhost;Database=psaas-tests");
        builder.ConfigureTestServices(services =>
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // A static configuration stops JwtBearer from downloading Keycloak metadata.
                options.Configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                options.Configuration.SigningKeys.Add(SigningKey);
            }));
    }

    public static string CreateToken(string audience = Audience, params string[] realmRoles)
    {
        var claims = new Dictionary<string, object>
        {
            ["preferred_username"] = "test.user",
            ["realm_access"] = new Dictionary<string, object> { ["roles"] = realmRoles }
        };

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())]),
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        });
    }
}
