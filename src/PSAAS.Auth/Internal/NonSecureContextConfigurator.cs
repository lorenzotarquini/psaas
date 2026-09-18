using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace PSAAS.Auth.Internal;

internal static class NonSecureContextConfigurator
{
    public static void ConfigureCookie(CookieAuthenticationOptions options)
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        options.Cookie.HttpOnly = true;
        options.Cookie.Path = "/";
    }

    public static void ConfigureOpenIdConnect(OpenIdConnectOptions options, IHostEnvironment environment, ILogger logger)
    {
        var handler = new HttpClientHandler();
        if (ServerCertificateValidationGuard.TryApplyDangerousServerCertificateValidation(handler, false, environment, logger, "OIDC backchannel"))
        {
            options.BackchannelHttpHandler = handler;
        }

        options.ResponseMode = OpenIdConnectResponseMode.Query;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.None;
        options.NonceCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.None;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    }

    public static void ConfigureJwtBearer(JwtBearerOptions options, IHostEnvironment environment, ILogger logger)
    {
        var handler = new HttpClientHandler();
        if (ServerCertificateValidationGuard.TryApplyDangerousServerCertificateValidation(handler, false, environment, logger, "JWT bearer backchannel"))
        {
            options.BackchannelHttpHandler = handler;
        }
    }
}
