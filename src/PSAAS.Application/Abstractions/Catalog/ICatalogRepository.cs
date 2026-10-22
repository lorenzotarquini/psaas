using PSAAS.Application.Contracts.Catalog;

namespace PSAAS.Application.Abstractions.Catalog;

/// <summary>Modules and plans (EXP-01/02).</summary>
public interface ICatalogRepository
{
    Task<IReadOnlyList<ModuleReadModel>> GetModulesAsync(CancellationToken cancellationToken = default);

    Task<ModuleReadModel?> GetModuleAsync(string moduleId, CancellationToken cancellationToken = default);

    /// <summary>Inserts the module or updates it to the manifest (a new version replaces the current one).</summary>
    Task SaveModuleAsync(ModuleManifest manifest, CancellationToken cancellationToken = default);

    Task<bool> SetModuleStatusAsync(string moduleId, string status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlanReadModel>> GetPlansAsync(CancellationToken cancellationToken = default);

    Task<PlanReadModel?> GetPlanAsync(Guid planId, CancellationToken cancellationToken = default);

    Task<bool> IsPlanCodeTakenAsync(string code, Guid? exceptPlanId, CancellationToken cancellationToken = default);

    Task<Guid> SavePlanAsync(Guid? planId, PlanRequest plan, CancellationToken cancellationToken = default);
}

/// <summary>Subscriptions and the modules of each tenant (EXP-03).</summary>
public interface ISubscriptionRepository
{
    Task<SubscriptionReadModel?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionReadModel>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Stores the subscription and the wanted module set; modules no longer wanted are removed.</summary>
    Task SaveAsync(SubscriptionReadModel subscription, IReadOnlyCollection<string> moduleIds, CancellationToken cancellationToken = default);

    Task SetModuleStatusAsync(Guid tenantId, string moduleId, string status, string? error, CancellationToken cancellationToken = default);

    /// <summary>Modules in Failed with fewer than <paramref name="maxAttempts"/> attempts, for the retry job.</summary>
    Task<IReadOnlyList<(Guid TenantId, string ModuleId)>> GetFailedAsync(int maxAttempts, CancellationToken cancellationToken = default);
}

/// <summary>Prepares a module for a tenant by calling its provisioning hook (HTTP); simulated in tests.</summary>
public interface IModuleProvisioner
{
    Task<ProvisioningOutcome> ProvisionAsync(ModuleReadModel module, Guid tenantId, CancellationToken cancellationToken = default);
}
