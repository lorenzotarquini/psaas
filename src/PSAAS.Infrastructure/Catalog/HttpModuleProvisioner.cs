using System.Net.Http.Json;
using NLog;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Contracts.Catalog;

namespace PSAAS.Infrastructure.Catalog;

/// <summary>
/// Calls the provisioning hook of a module (POST { tenantId, moduleId, version }). A module without hook has nothing
/// to prepare; a non-2xx answer, a timeout or a network error is a failure kept with its message for the retry.
/// </summary>
public sealed class HttpModuleProvisioner(HttpClient http) : IModuleProvisioner
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<ProvisioningOutcome> ProvisionAsync(ModuleReadModel module, Guid tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(module);
        if (string.IsNullOrWhiteSpace(module.ProvisionUrl))
        {
            return new ProvisioningOutcome(true, null);
        }

        try
        {
            using var response = await http.PostAsJsonAsync(
                module.ProvisionUrl, new { tenantId, moduleId = module.Id, version = module.Version }, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                Logger.Info("Module {ModuleId} provisioned for tenant {TenantId}", module.Id, tenantId);
                return new ProvisioningOutcome(true, null);
            }

            return new ProvisioningOutcome(false, $"hook provision → HTTP {(int)response.StatusCode}");
        }
        catch (Exception exception) when (exception is HttpRequestException || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            Logger.Warn(exception, "Provisioning hook of module {ModuleId} unreachable", module.Id);
            return new ProvisioningOutcome(false, exception is TaskCanceledException ? "hook provision → timeout" : "hook provision → non raggiungibile");
        }
    }
}
