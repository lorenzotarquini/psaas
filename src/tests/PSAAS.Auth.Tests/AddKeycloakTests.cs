using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace PSAAS.Auth.Tests;

public sealed class AddKeycloakTests
{
    [Fact]
    public void AddKeycloak_ApiMode_ConfiguresJwtBearerAndPolicy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());

        services.AddKeycloak(options =>
        {
            options.Authority = "https://keycloak.example/realms/psaas";
            options.ClientId = "psaas-api";
            options.Mode = KeycloakAuthenticationMode.Api;
            options.Policies.Add(new KeycloakPolicyOptions
            {
                Name = "AdminOnly",
                Roles = ["SystemAdmin"]
            });
        });

        using var provider = services.BuildServiceProvider();
        var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Bearer");

        Assert.Equal("https://keycloak.example/realms/psaas", jwtOptions.Authority);
        Assert.False(jwtOptions.TokenValidationParameters.ValidateAudience);
        Assert.Equal(System.Security.Claims.ClaimTypes.Role, jwtOptions.TokenValidationParameters.RoleClaimType);
    }

    [Fact]
    public void AddKeycloak_WebModeWithoutSecret_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddKeycloak(options =>
        {
            options.Authority = "https://keycloak.example/realms/psaas";
            options.ClientId = "psaas-web";
            options.Mode = KeycloakAuthenticationMode.Web;
        }));
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "PSAAS.Auth.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
