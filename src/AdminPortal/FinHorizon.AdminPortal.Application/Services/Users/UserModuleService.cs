using NLog;
using FinHorizon.AdminPortal.Application.Abstractions.Catalog;
using FinHorizon.AdminPortal.Application.Abstractions.Users;
using FinHorizon.AdminPortal.Application.Contracts.Audit;
using FinHorizon.AdminPortal.Application.Contracts.Tenants;
using FinHorizon.Modules.Sdk;

namespace FinHorizon.AdminPortal.Application.Services.Users;

/// <param name="ModuleIds">The modules the user has after the operation; empty when it was rejected.</param>
public sealed record UserModulesResult(bool Found, IReadOnlyList<string> ModuleIds, IReadOnlyDictionary<string, string> Errors)
{
    public static readonly UserModulesResult NotFound = new(false, [], new Dictionary<string, string>());

    public bool Succeeded => Found && Errors.Count == 0;

    public static UserModulesResult Ok(IReadOnlyList<string>? moduleIds) => new(true, moduleIds ?? [], new Dictionary<string, string>());
}

/// <summary>
/// UC32: the administrator gives a module to one user (e.g. MySync) or takes it away. The module is the access:
/// without it the user cannot enter the module at all.
/// </summary>
public sealed class UserModuleService(IUserDirectory directory, ICatalogRepository catalog, UserManagementService users, UserRules rules)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    public async Task<IReadOnlyList<string>?> GetAsync(string userId, CancellationToken cancellationToken = default) =>
        await directory.GetModulesAsync(userId, cancellationToken);

    /// <summary>UC32-R1: only a module of the catalog that is not deprecated.</summary>
    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    public async Task<UserModulesResult> GrantAsync(string userId, string moduleId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetAsync(userId, cancellationToken);
        if (user is null) return UserModulesResult.NotFound;

        var module = await catalog.GetModuleAsync(moduleId, cancellationToken);
        if (module is null || module.Status == ModuleStatuses.Deprecated)
        {
            Logger.Info("Module {ModuleId} cannot be given to user {UserId}: unknown or deprecated", moduleId, userId);
            return new UserModulesResult(true, [], new Dictionary<string, string> { ["moduleId"] = TenantFieldValidationErrorCodes.InvalidValue });
        }

        var before = await directory.GetModulesAsync(userId, cancellationToken) ?? [];
        if (!before.Contains(module.Id))
        {
            await directory.GrantModuleAsync(userId, module.Id, cancellationToken);
            await rules.RecordAsync(AuditActions.UserModuleGranted, user, _ => new { email = user.Email, module = module.Name }, cancellationToken);
            Logger.Info("Module {ModuleId} given to user {UserId}", module.Id, userId);
        }

        return UserModulesResult.Ok(await directory.GetModulesAsync(userId, cancellationToken));
    }

    /// <summary>UC32-R2: the sessions of the user end too, so the module stops working within minutes.</summary>
    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    public async Task<UserModulesResult> RevokeAsync(string userId, string moduleId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetAsync(userId, cancellationToken);
        if (user is null) return UserModulesResult.NotFound;

        var before = await directory.GetModulesAsync(userId, cancellationToken) ?? [];
        if (before.Contains(moduleId))
        {
            await directory.RevokeModuleAsync(userId, moduleId, cancellationToken);
            var name = (await catalog.GetModuleAsync(moduleId, cancellationToken))?.Name ?? moduleId;
            await rules.RecordAsync(AuditActions.UserModuleRevoked, user, _ => new { email = user.Email, module = name }, cancellationToken);
            Logger.Info("Module {ModuleId} taken away from user {UserId}", moduleId, userId);
        }

        return UserModulesResult.Ok(await directory.GetModulesAsync(userId, cancellationToken));
    }
}
