using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using PSAAS.Auth.Internal;

namespace PSAAS.Auth;

public static class KeycloakServiceCollectionExtensions
{
    public static IServiceCollection AddKeycloak(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return services.AddKeycloak(options => configuration.GetSection(KeycloakOptions.DefaultSectionName).Bind(options));
    }

    public static IServiceCollection AddKeycloak(this IServiceCollection services, Action<KeycloakOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new KeycloakOptions();
        configure(options);
        KeycloakOptionsValidator.Validate(options);

        services.AddHttpContextAccessor();
        services.AddOptions<KeycloakOptions>().Configure(configure);

        var authBuilder = ConfigureAuthentication(services, options);
        ConfigureAuthorization(services, options);

        if (options.Mode == KeycloakAuthenticationMode.MachineToMachine)
        {
            ConfigureClientCredentials(services, options);
        }

        return services;
    }

    private static AuthenticationBuilder ConfigureAuthentication(IServiceCollection services, KeycloakOptions options)
    {
        var builder = services.AddAuthentication(authenticationOptions =>
        {
            if (options.Mode == KeycloakAuthenticationMode.Api)
            {
                authenticationOptions.DefaultAuthenticateScheme = options.JwtBearerScheme;
                authenticationOptions.DefaultChallengeScheme = options.JwtBearerScheme;
            }
            else if (options.Mode is KeycloakAuthenticationMode.Web or KeycloakAuthenticationMode.ApiAndWeb)
            {
                authenticationOptions.DefaultScheme = options.CookieScheme;
                authenticationOptions.DefaultAuthenticateScheme = options.CookieScheme;
                authenticationOptions.DefaultChallengeScheme = options.OpenIdConnectScheme;
                authenticationOptions.DefaultSignInScheme = options.CookieScheme;
            }
        });

        if (options.Mode is KeycloakAuthenticationMode.Api or KeycloakAuthenticationMode.ApiAndWeb)
        {
            builder.AddJwtBearer(options.JwtBearerScheme, _ => { });
            ConfigureJwtBearerOptions(services, options.JwtBearerScheme);
        }

        if (options.Mode is KeycloakAuthenticationMode.Web or KeycloakAuthenticationMode.ApiAndWeb)
        {
            builder.AddCookie(options.CookieScheme, _ => { });
            builder.AddOpenIdConnect(options.OpenIdConnectScheme, _ => { });
            ConfigureCookieOptions(services, options.CookieScheme);
            ConfigureOpenIdConnectOptions(services, options.OpenIdConnectScheme);
        }

        return builder;
    }

    private static void ConfigureJwtBearerOptions(IServiceCollection services, string scheme)
    {
        services.AddOptions<JwtBearerOptions>(scheme)
            .Configure<IOptions<KeycloakOptions>, IHostEnvironment, ILoggerFactory>((jwtOptions, keycloakOptionsAccessor, environment, loggerFactory) =>
            {
                var keycloakOptions = keycloakOptionsAccessor.Value;
                jwtOptions.Authority = keycloakOptions.Authority.TrimEnd('/');
                jwtOptions.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
                jwtOptions.RefreshOnIssuerKeyNotFound = true;
                jwtOptions.MapInboundClaims = false;
                jwtOptions.TokenValidationParameters.ValidateAudience = keycloakOptions.ValidateAudience;
                jwtOptions.TokenValidationParameters.ValidAudiences = keycloakOptions.ValidAudiences;
                jwtOptions.TokenValidationParameters.ValidIssuer = keycloakOptions.Authority.TrimEnd('/');
                jwtOptions.TokenValidationParameters.RoleClaimType = keycloakOptions.RoleClaimType;
                jwtOptions.TokenValidationParameters.NameClaimType = keycloakOptions.NameClaimType;
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity && context.SecurityToken is JwtSecurityToken token)
                        {
                            KeycloakRoleClaims.AddRoleClaims(identity, token, keycloakOptions);
                        }

                        keycloakOptions.OnJwtBearerTokenValidated?.Invoke(context);
                        return Task.CompletedTask;
                    }
                };

                if (!keycloakOptions.RequireHttpsMetadata)
                {
                    NonSecureContextConfigurator.ConfigureJwtBearer(jwtOptions, environment, loggerFactory.CreateLogger("PSAAS.Auth"));
                }
            });
    }

    private static void ConfigureCookieOptions(IServiceCollection services, string scheme)
    {
        services.AddOptions<CookieAuthenticationOptions>(scheme)
            .Configure<IOptions<KeycloakOptions>>((cookieOptions, keycloakOptionsAccessor) =>
            {
                if (!keycloakOptionsAccessor.Value.RequireHttpsMetadata)
                {
                    NonSecureContextConfigurator.ConfigureCookie(cookieOptions);
                }
            });
    }

    private static void ConfigureOpenIdConnectOptions(IServiceCollection services, string scheme)
    {
        services.AddOptions<OpenIdConnectOptions>(scheme)
            .Configure<IOptions<KeycloakOptions>, IHostEnvironment, ILoggerFactory>((oidcOptions, keycloakOptionsAccessor, environment, loggerFactory) =>
            {
                var keycloakOptions = keycloakOptionsAccessor.Value;
                var authority = keycloakOptions.Authority.TrimEnd('/');
                oidcOptions.Authority = authority;
                oidcOptions.MetadataAddress = $"{authority}/.well-known/openid-configuration";
                oidcOptions.ClientId = keycloakOptions.ClientId;
                oidcOptions.ClientSecret = keycloakOptions.ClientSecret;
                oidcOptions.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
                oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
                oidcOptions.GetClaimsFromUserInfoEndpoint = true;
                oidcOptions.SaveTokens = keycloakOptions.SaveTokens;
                oidcOptions.MapInboundClaims = false;
                oidcOptions.SignInScheme = keycloakOptions.CookieScheme;
                oidcOptions.SignOutScheme = keycloakOptions.CookieScheme;
                oidcOptions.CallbackPath = keycloakOptions.CallbackPath;
                oidcOptions.SignedOutCallbackPath = keycloakOptions.SignedOutCallbackPath;
                oidcOptions.RemoteSignOutPath = keycloakOptions.RemoteSignOutPath;
                oidcOptions.TokenValidationParameters.RoleClaimType = keycloakOptions.RoleClaimType;
                oidcOptions.TokenValidationParameters.NameClaimType = keycloakOptions.NameClaimType;
                oidcOptions.Scope.Clear();
                foreach (var scope in keycloakOptions.Scopes.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    oidcOptions.Scope.Add(scope);
                }

                oidcOptions.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = context =>
                    {
                        AddAccessTokenClaims(context, keycloakOptions);
                        keycloakOptions.OnOpenIdConnectTokenValidated?.Invoke(context);
                        return Task.CompletedTask;
                    },
                    OnRemoteFailure = context =>
                    {
                        context.HandleResponse();
                        context.Response.Redirect("/");
                        return Task.CompletedTask;
                    }
                };

                if (!keycloakOptions.RequireHttpsMetadata)
                {
                    NonSecureContextConfigurator.ConfigureOpenIdConnect(oidcOptions, environment, loggerFactory.CreateLogger("PSAAS.Auth"));
                }
            });
    }

    private static void ConfigureAuthorization(IServiceCollection services, KeycloakOptions options)
    {
        services.AddAuthorization(authorizationOptions =>
        {
            foreach (var policyOptions in options.Policies.Where(policy => !string.IsNullOrWhiteSpace(policy.Name)))
            {
                authorizationOptions.AddPolicy(policyOptions.Name, policy =>
                {
                    if (policyOptions.RequireAuthenticatedUser)
                    {
                        policy.RequireAuthenticatedUser();
                    }

                    var schemes = policyOptions.AuthenticationSchemes.Length > 0
                        ? policyOptions.AuthenticationSchemes
                        : DefaultPolicySchemes(options);
                    policy.AddAuthenticationSchemes(schemes);

                    if (policyOptions.Roles.Length > 0)
                    {
                        policy.RequireRole(policyOptions.Roles);
                    }

                    foreach (var claim in policyOptions.Claims)
                    {
                        policy.RequireClaim(claim.Key, claim.Value);
                    }
                });
            }
        });
    }

    private static void ConfigureClientCredentials(IServiceCollection services, KeycloakOptions options)
    {
        services.AddTransient<KeycloakClientCredentialsHandler>();
        services.AddHttpClient(KeycloakDefaults.TokenHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                var keycloakOptions = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
                var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
                var logger = serviceProvider.GetRequiredService<ILogger<KeycloakClientCredentialsHandler>>();
                var handler = new HttpClientHandler();
                ServerCertificateValidationGuard.TryApplyDangerousServerCertificateValidation(handler, keycloakOptions.RequireHttpsMetadata, environment, logger, "client credentials token acquisition");
                return handler;
            });

        services.AddHttpClient(options.ClientCredentials.HttpClientName)
            .AddHttpMessageHandler<KeycloakClientCredentialsHandler>();
    }

    private static string[] DefaultPolicySchemes(KeycloakOptions options) => options.Mode switch
    {
        KeycloakAuthenticationMode.Api => [options.JwtBearerScheme],
        KeycloakAuthenticationMode.Web => [options.CookieScheme],
        KeycloakAuthenticationMode.ApiAndWeb => [options.JwtBearerScheme, options.CookieScheme],
        _ => []
    };

    private static void AddAccessTokenClaims(Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context, KeycloakOptions options)
    {
        var accessToken = context.TokenEndpointResponse?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken) || context.Principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(accessToken))
        {
            return;
        }

        var jwt = handler.ReadJwtToken(accessToken);
        foreach (var claim in jwt.Claims.Where(claim => !string.Equals(claim.Type, "scope", StringComparison.OrdinalIgnoreCase)))
        {
            if (!identity.HasClaim(claim.Type, claim.Value))
            {
                identity.AddClaim(claim);
            }
        }

        KeycloakRoleClaims.AddRoleClaims(identity, jwt, options);
    }
}
