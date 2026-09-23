using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace PSAAS.Auth;

public static class KeycloakEndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder MapKeycloakLogout(this IEndpointRouteBuilder endpoints)
        => endpoints.MapKeycloakLogout(KeycloakDefaults.LogoutPath);

    public static RouteHandlerBuilder MapKeycloakLogout(this IEndpointRouteBuilder endpoints, string pattern)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Logout endpoint pattern is required.", nameof(pattern));
        }

        var builder = endpoints.MapPost(pattern, (Delegate)HandleLogoutAsync);
        builder.RequireAuthorization();
        return builder;
    }

    private static async Task<IResult> HandleLogoutAsync(HttpContext httpContext)
    {
        var keycloakOptions = httpContext.RequestServices.GetRequiredService<IOptions<KeycloakOptions>>().Value;
        if (!IsWebMode(keycloakOptions.Mode))
        {
            return Results.NotFound();
        }

        var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
        await antiforgery.ValidateRequestAsync(httpContext);

        var properties = new AuthenticationProperties
        {
            RedirectUri = GetSafeLocalRedirectPath(keycloakOptions.LogoutRedirectPath)
        };

        return Results.SignOut(
            properties,
            [keycloakOptions.CookieScheme, keycloakOptions.OpenIdConnectScheme]);
    }

    private static bool IsWebMode(KeycloakAuthenticationMode mode)
        => mode is KeycloakAuthenticationMode.Web or KeycloakAuthenticationMode.ApiAndWeb;

    private static string GetSafeLocalRedirectPath(string? redirectPath)
    {
        if (string.IsNullOrWhiteSpace(redirectPath))
        {
            return "/";
        }

        if (!Uri.TryCreate(redirectPath, UriKind.Relative, out _)
            || !redirectPath.StartsWith("/", StringComparison.Ordinal)
            || redirectPath.StartsWith("//", StringComparison.Ordinal)
            || redirectPath.StartsWith("/\\", StringComparison.Ordinal))
        {
            return "/";
        }

        return redirectPath;
    }
}
