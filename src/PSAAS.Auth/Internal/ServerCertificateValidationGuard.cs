using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PSAAS.Auth.Internal;

internal static class ServerCertificateValidationGuard
{
    public static bool TryApplyDangerousServerCertificateValidation(
        HttpClientHandler handler,
        bool requireHttpsMetadata,
        IHostEnvironment hostEnvironment,
        ILogger logger,
        string context)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(logger);

        if (requireHttpsMetadata)
        {
            return false;
        }

        if (!hostEnvironment.IsDevelopment() && !hostEnvironment.IsStaging())
        {
            logger.LogCritical(
                "RequireHttpsMetadata is false in environment {EnvironmentName}. TLS certificate validation bypass for {Context} was not applied.",
                hostEnvironment.EnvironmentName,
                context);
            return false;
        }

        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        logger.LogInformation("TLS certificate validation bypass enabled for {Context} in environment {EnvironmentName}.", context, hostEnvironment.EnvironmentName);
        return true;
    }
}
