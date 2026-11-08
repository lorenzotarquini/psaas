namespace FinHorizon.Modules.Sdk;

/// <summary>
/// Body of the POST the platform sends to <see cref="ModuleManifest.ProvisionUrl" /> when the module is activated
/// for a tenant (UC18): the module prepares that tenant's data and answers 2xx.
/// </summary>
public sealed record ModuleProvisioningRequest(Guid TenantId, string ModuleId, string Version);
