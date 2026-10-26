using NLog;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Application.Contracts.Catalog;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Services.Billing;
using PSAAS.Application.Services.Tenants;
using PSAAS.Domain;

namespace PSAAS.Application.Services.Catalog;

/// <summary>
/// UC17 (EXP-03): the plan of a tenant and the modules it gets from it. The values of the plan are frozen
/// in the subscription (later plan changes do not touch it), modules come with their dependencies and core,
/// new modules are provisioned through their hook; a failure is kept with its error and can be retried (UC18).
/// </summary>
public sealed class SubscriptionService(
    ITenantReadRepository tenants,
    ICatalogRepository catalog,
    ISubscriptionRepository subscriptions,
    IModuleProvisioner provisioner,
    IUserDirectory users,
    TenantLifecycleService lifecycle,
    BillingService billing,
    IAuditLog audit,
    TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const int MaxProvisioningAttempts = 3;

    public async Task<TenantEditResult> SubscribeAsync(Guid tenantId, SubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenant = await tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null) return TenantEditResult.NotFound;

        var errors = new Dictionary<string, string>();
        var plan = request.PlanId is { } planId ? await catalog.GetPlanAsync(planId, cancellationToken) : null;
        if (plan is null || !plan.Active) errors["planId"] = request.PlanId is null ? TenantFieldValidationErrorCodes.Required : TenantFieldValidationErrorCodes.InvalidValue;
        var modules = (await catalog.GetModulesAsync(cancellationToken)).ToDictionary(m => m.Id);
        var addOns = (request.AddOnModuleIds ?? []).ToArray();
        for (var i = 0; i < addOns.Length; i++)
        {
            if (!modules.ContainsKey(addOns[i])) errors[$"addOnModuleIds[{i}]"] = TenantFieldValidationErrorCodes.InvalidValue;
        }

        if (plan is not null && errors.Count == 0)
        {
            await CheckLimitsAsync(tenantId, plan, errors, cancellationToken);
        }

        if (errors.Count > 0)
        {
            Logger.Info("Subscription of tenant {TenantId} rejected: {ValidationErrors}", tenantId, errors);
            return new TenantEditResult(true, errors);
        }

        var current = await subscriptions.GetAsync(tenantId, cancellationToken);
        var now = clock.GetUtcNow();
        var trial = request.Trial && current is null && plan!.TrialDays > 0;
        var period = plan!.BillingPeriod == BillingPeriods.Yearly ? TimeSpan.FromDays(365) : TimeSpan.FromDays(30);
        // A plan change keeps the running period; a trial converts on its end date.
        var startsAt = current?.StartsAt ?? now;
        var trialEndsAt = trial ? now.AddDays(plan.TrialDays) : null as DateTimeOffset?;
        var renewsAt = trial ? trialEndsAt!.Value : current is { Status: SubscriptionStatuses.Active } ? current.RenewsAt : now.Add(period);
        var wanted = WithDependencies(plan.ModuleIds.Concat(addOns).Append(CatalogService.CoreModuleId), modules);
        var subscription = new SubscriptionReadModel(
            tenantId, tenant.CompanyName, plan.Id, plan.Name,
            trial ? SubscriptionStatuses.Trial : SubscriptionStatuses.Active,
            plan.Price, plan.BillingPeriod, plan.MaxUsers, plan.MaxProductiveUnits,
            startsAt, trialEndsAt, renewsAt, request.AutoRenew, addOns.Distinct().ToArray(), []);
        await subscriptions.SaveAsync(subscription, wanted, cancellationToken);

        // A paid subscription ends the trial of the tenant ("Converti", mock 1b).
        if (!trial && tenant.Lifecycle.Status == TenantStatus.Trial)
        {
            await lifecycle.ChangeStatusAsync(tenantId, new TenantStatusChangeRequest(nameof(TenantStatus.Active), null), cancellationToken);
        }

        // UC23: a first paid period is invoiced; a change of plan in a paid period pays (or credits) the difference.
        if (!trial)
        {
            if (current is { Status: SubscriptionStatuses.Active }) await billing.InvoicePlanChangeAsync(current, subscription, cancellationToken);
            else await billing.InvoicePeriodAsync(subscription, now, cancellationToken);
        }

        var before = current?.Modules.Where(m => m.Status == ProvisioningStatuses.Active).Select(m => m.ModuleId).ToHashSet() ?? [];
        foreach (var moduleId in wanted.Where(id => !before.Contains(id)))
        {
            await ProvisionAsync(tenantId, tenant.CompanyName, modules[moduleId], cancellationToken);
        }

        await audit.RecordAsync(
            new AuditRecord(
                AuditActions.SubscriptionChanged, AuditTargets.Tenant, tenantId.ToString(), tenant.CompanyName, tenantId.ToString(),
                new { plan = plan.Name, previousPlan = current?.PlanName, trial, modulesAdded = wanted.Except(before).ToArray(), modulesRemoved = before.Except(wanted).ToArray() }),
            cancellationToken);
        Logger.Info("Tenant {TenantId} subscribed to plan {Plan} (trial {Trial}) with modules {Modules}", tenantId, plan.Code, trial, wanted);
        return TenantEditResult.Ok;
    }

    /// <summary>UC18: provisions again a module that failed.</summary>
    public async Task<TenantEditResult> RetryAsync(Guid tenantId, string moduleId, CancellationToken cancellationToken = default)
    {
        var subscription = await subscriptions.GetAsync(tenantId, cancellationToken);
        var tenantModule = subscription?.Modules.FirstOrDefault(m => m.ModuleId == moduleId);
        var module = await catalog.GetModuleAsync(moduleId, cancellationToken);
        if (subscription is null || tenantModule is null || module is null) return TenantEditResult.NotFound;
        if (tenantModule.Status == ProvisioningStatuses.Active)
        {
            return new TenantEditResult(true, new Dictionary<string, string> { ["moduleId"] = TenantFieldValidationErrorCodes.InvalidValue });
        }

        await ProvisionAsync(tenantId, subscription.TenantName, module, cancellationToken);
        return TenantEditResult.Ok;
    }

    /// <summary>The scheduled job: modules still Failed after fewer than <see cref="MaxProvisioningAttempts"/> attempts.</summary>
    public async Task<int> RetryFailedAsync(CancellationToken cancellationToken = default)
    {
        var failed = await subscriptions.GetFailedAsync(MaxProvisioningAttempts, cancellationToken);
        foreach (var (tenantId, moduleId) in failed)
        {
            await RetryAsync(tenantId, moduleId, cancellationToken);
        }

        return failed.Count;
    }

    private async Task ProvisionAsync(Guid tenantId, string tenantName, ModuleReadModel module, CancellationToken cancellationToken)
    {
        await subscriptions.SetModuleStatusAsync(tenantId, module.Id, ProvisioningStatuses.Provisioning, null, cancellationToken);
        var outcome = await provisioner.ProvisionAsync(module, tenantId, cancellationToken);
        await subscriptions.SetModuleStatusAsync(
            tenantId, module.Id, outcome.Succeeded ? ProvisioningStatuses.Active : ProvisioningStatuses.Failed, outcome.Error, cancellationToken);
        await audit.RecordAsync(
            new AuditRecord(
                outcome.Succeeded ? AuditActions.ModuleProvisioned : AuditActions.ModuleProvisioningFailed,
                AuditTargets.Tenant, tenantId.ToString(), tenantName, tenantId.ToString(),
                new { module = module.Name, version = module.Version, error = outcome.Error }),
            cancellationToken);
        if (!outcome.Succeeded)
        {
            Logger.Warn("Provisioning of module {ModuleId} for tenant {TenantId} failed: {Error}", module.Id, tenantId, outcome.Error);
        }
    }

    // EXP-02: a plan cannot be chosen if the tenant already has more users or units than it allows.
    private async Task CheckLimitsAsync(Guid tenantId, PlanReadModel plan, Dictionary<string, string> errors, CancellationToken cancellationToken)
    {
        var structure = await tenants.GetProfilesAndProductiveUnitsAsync(tenantId.ToString(), cancellationToken);
        if (plan.MaxProductiveUnits is { } maxUnits && structure.ProductiveUnits.Count > maxUnits)
        {
            errors["planId"] = TenantFieldValidationErrorCodes.LimitExceeded;
        }

        if (plan.MaxUsers is { } maxUsers && structure.Profiles.Count > 0)
        {
            try
            {
                if (await users.CountUsersWithAnyAsync(structure.Profiles.Select(p => p.Id).ToArray(), cancellationToken) > maxUsers)
                {
                    errors["planId"] = TenantFieldValidationErrorCodes.LimitExceeded;
                }
            }
            catch (UserDirectoryUnavailableException exception)
            {
                // Not blocking: the limit is checked again on every new user (UC6).
                Logger.Warn(exception, "User limit of tenant {TenantId} not checked: Keycloak unreachable", tenantId);
            }
        }
    }

    /// <summary>The modules with everything they require, transitively.</summary>
    public static IReadOnlyCollection<string> WithDependencies(IEnumerable<string> moduleIds, IReadOnlyDictionary<string, ModuleReadModel> modules)
    {
        var result = new HashSet<string>();
        var pending = new Stack<string>(moduleIds);
        while (pending.Count > 0)
        {
            var id = pending.Pop();
            if (!result.Add(id) || !modules.TryGetValue(id, out var module)) continue;
            foreach (var required in module.Requires) pending.Push(required);
        }

        return result.Where(modules.ContainsKey).ToArray();
    }
}
