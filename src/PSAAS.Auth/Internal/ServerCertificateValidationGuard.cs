using Microsoft.Extensions.Hosting;
using NLog;

namespace PSAAS.Auth.Internal;

internal static class ServerCertificateValidationGuard
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static bool TryApplyDangerousServerCertificateValidation(
        HttpClientHandler handler,
        bool requireHttpsMetadata,
        IHostEnvironment hostEnvironment,
        string context)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        if (requireHttpsMetadata)
        {
            return false;
        }

        if (!hostEnvironment.IsDevelopment() && !hostEnvironment.IsStaging())
        {
            Logger.Fatal(
                "RequireHttpsMetadata is false in environment {EnvironmentName}. TLS certificate validation bypass for {Context} was not applied.",
                hostEnvironment.EnvironmentName,
                context);
            return false;
        }

        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        Logger.Info("TLS certificate validation bypass enabled for {Context} in environment {EnvironmentName}.", context, hostEnvironment.EnvironmentName);
        return true;
    }
}
