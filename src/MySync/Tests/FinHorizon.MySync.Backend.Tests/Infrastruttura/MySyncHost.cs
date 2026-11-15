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

namespace FinHorizon.MySync.Backend.Tests.Infrastruttura;

/// <summary>
/// The real backend; Keycloak is played by a local signing key. The database is never opened (it is still empty),
/// so the connection string only has to be there.
/// </summary>
public sealed class MySyncHost : WebApplicationFactory<Program>
{
    public const string Issuer = "https://auth.fin-horizon.com/realms/PSAAS";
    public const string Audience = "mysync-api";

    private static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("fnhz-mysync-tests-signing-key-0123456789"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:mysync-db", "Host=localhost;Database=mysync-db;Username=unused;Password=unused");
        builder.ConfigureTestServices(services => services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
            options.Configuration.SigningKeys.Add(SigningKey);
        }));
    }

    /// <summary>A token like Keycloak's for a user of the realm, without any role.</summary>
    public static string Token(string audience = Audience) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Issuer = Issuer,
        Audience = audience,
        Subject = new ClaimsIdentity([new Claim("sub", "kc-mario")]),
        Claims = new Dictionary<string, object> { ["preferred_username"] = "mario.rossi", ["email"] = "mario.rossi@example.com" },
        Expires = DateTime.UtcNow.AddMinutes(5),
        SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
    });
}
