using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
    public void AddKeycloak_WebModeWithoutSecret_ConfiguresOpenIdConnect()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());

        services.AddKeycloak(options =>
        {
            options.Authority = "https://keycloak.example/realms/psaas";
            options.ClientId = "psaas-web";
            options.Mode = KeycloakAuthenticationMode.Web;
        });

        using var provider = services.BuildServiceProvider();
        var oidcOptions = provider.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>>().Get("OpenIdConnect");

        Assert.Equal("psaas-web", oidcOptions.ClientId);
        Assert.Null(oidcOptions.ClientSecret);
        Assert.NotNull(provider.GetService<IAntiforgery>());
    }

    [Fact]
    public void AddKeycloak_WebModePolicy_DoesNotOverrideOidcChallengeScheme()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());

        services.AddKeycloak(options =>
        {
            options.Authority = "https://keycloak.example/realms/psaas";
            options.ClientId = "psaas-web";
            options.Mode = KeycloakAuthenticationMode.Web;
            options.Policies.Add(new KeycloakPolicyOptions
            {
                Name = "Administrator",
                Roles = ["Administrator"]
            });
        });

        using var provider = services.BuildServiceProvider();
        var authenticationOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        var authorizationOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var policy = authorizationOptions.GetPolicy("Administrator");

        Assert.Equal("Cookies", authenticationOptions.DefaultScheme);
        Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, authenticationOptions.DefaultChallengeScheme);
        Assert.NotNull(policy);
        Assert.Empty(policy.AuthenticationSchemes);
    }

    [Fact]
    public void MapKeycloakLogout_MapsStablePostEndpointRequiringAuthorization()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddLogging();
        builder.Services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        builder.Services.AddKeycloak(options =>
        {
            options.Authority = "https://keycloak.example/realms/psaas";
            options.ClientId = "psaas-web";
            options.Mode = KeycloakAuthenticationMode.Web;
        });

        using var app = builder.Build();
        app.MapKeycloakLogout();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(routeEndpoint => routeEndpoint.RoutePattern.RawText == KeycloakDefaults.LogoutPath);

        var httpMethods = endpoint.Metadata.GetRequiredMetadata<IHttpMethodMetadata>();

        Assert.Equal([HttpMethods.Post], httpMethods.HttpMethods);
        Assert.Contains(endpoint.Metadata, metadata => metadata is IAuthorizeData);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "PSAAS.Auth.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
