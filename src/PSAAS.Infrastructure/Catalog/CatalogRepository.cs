using Microsoft.EntityFrameworkCore;
using NLog;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Contracts.Catalog;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Catalog;

public sealed class CatalogRepository(PsaasDbContext dbContext, TimeProvider clock) : ICatalogRepository, ISubscriptionRepository
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<IReadOnlyList<ModuleReadModel>> GetModulesAsync(CancellationToken cancellationToken = default)
    {
        var usage = await dbContext.TenantModules.AsNoTracking()
            .GroupBy(m => m.ModuleId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken)
            .ConfigureAwait(false);
        var modules = await dbContext.Modules.AsNoTracking()
            .OrderByDescending(m => m.IsCore).ThenBy(m => m.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return modules.Select(m => ToReadModel(m, usage.GetValueOrDefault(m.Id))).ToArray();
    }

    public async Task<ModuleReadModel?> GetModuleAsync(string moduleId, CancellationToken cancellationToken = default)
    {
        var module = await dbContext.Modules.AsNoTracking().SingleOrDefaultAsync(m => m.Id == moduleId, cancellationToken).ConfigureAwait(false);
        if (module is null) return null;
        var count = await dbContext.TenantModules.CountAsync(m => m.ModuleId == moduleId, cancellationToken).ConfigureAwait(false);
        return ToReadModel(module, count);
    }

    public async Task SaveModuleAsync(ModuleManifest manifest, CancellationToken cancellationToken = default)
    {
        var module = await dbContext.Modules.SingleOrDefaultAsync(m => m.Id == manifest.Id, cancellationToken).ConfigureAwait(false);
        if (module is null)
        {
            module = new ModuleDbModel { Id = manifest.Id! };
            dbContext.Modules.Add(module);
        }

        module.Name = manifest.Name!;
        module.Description = manifest.Description;
        module.Icon = manifest.Icon;
        module.Status = manifest.Status!;
        module.Version = manifest.Version!;
        module.Requires = manifest.Requires?.ToArray() ?? [];
        module.Permissions = manifest.Permissions?.ToArray() ?? [];
        module.ProvisionUrl = manifest.ProvisionUrl;
        module.DeprovisionUrl = manifest.DeprovisionUrl;
        module.UpdatedAt = clock.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> SetModuleStatusAsync(string moduleId, string status, CancellationToken cancellationToken = default)
    {
        var module = await dbContext.Modules.SingleOrDefaultAsync(m => m.Id == moduleId, cancellationToken).ConfigureAwait(false);
        if (module is null) return false;
        module.Status = status;
        module.UpdatedAt = clock.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<PlanReadModel>> GetPlansAsync(CancellationToken cancellationToken = default)
    {
        var subscribers = await SubscriberCountsAsync(cancellationToken).ConfigureAwait(false);
        var plans = await dbContext.Plans.AsNoTracking().OrderBy(p => p.Price).ThenBy(p => p.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        return plans.Select(p => ToReadModel(p, subscribers.GetValueOrDefault(p.Id))).ToArray();
    }

    public async Task<PlanReadModel?> GetPlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await dbContext.Plans.AsNoTracking().SingleOrDefaultAsync(p => p.Id == planId, cancellationToken).ConfigureAwait(false);
        if (plan is null) return null;
        var count = await dbContext.Subscriptions.CountAsync(s => s.PlanId == planId, cancellationToken).ConfigureAwait(false);
        return ToReadModel(plan, count);
    }

    public Task<bool> IsPlanCodeTakenAsync(string code, Guid? exceptPlanId, CancellationToken cancellationToken = default) =>
        dbContext.Plans.AnyAsync(p => p.Code == code && p.Id != exceptPlanId, cancellationToken);

    public async Task<Guid> SavePlanAsync(Guid? planId, PlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = planId is null ? null : await dbContext.Plans.SingleOrDefaultAsync(p => p.Id == planId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            plan = new PlanDbModel { Id = Guid.NewGuid() };
            dbContext.Plans.Add(plan);
        }

        plan.Code = request.Code!;
        plan.Name = request.Name!;
        plan.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        plan.Price = request.Price!.Value;
        plan.BillingPeriod = request.BillingPeriod!;
        plan.TrialDays = request.TrialDays ?? 0;
        plan.MaxUsers = request.MaxUsers;
        plan.MaxProductiveUnits = request.MaxProductiveUnits;
        plan.ModuleIds = request.ModuleIds?.ToArray() ?? [];
        plan.Active = request.Active;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return plan.Id;
    }

    public async Task<SubscriptionReadModel?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var id = tenantId.ToString();
        var subscription = await dbContext.Subscriptions.AsNoTracking().SingleOrDefaultAsync(s => s.TenantId == id, cancellationToken).ConfigureAwait(false);
        return subscription is null ? null : (await ToReadModelsAsync([subscription], cancellationToken).ConfigureAwait(false))[0];
    }

    public async Task<IReadOnlyList<SubscriptionReadModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var all = await dbContext.Subscriptions.AsNoTracking().OrderBy(s => s.RenewsAt).ToListAsync(cancellationToken).ConfigureAwait(false);
        return await ToReadModelsAsync(all, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(SubscriptionReadModel subscription, IReadOnlyCollection<string> moduleIds, CancellationToken cancellationToken = default)
    {
        var tenantId = subscription.TenantId.ToString();
        var row = await dbContext.Subscriptions.SingleOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            row = new SubscriptionDbModel { TenantId = tenantId };
            dbContext.Subscriptions.Add(row);
        }

        row.PlanId = subscription.PlanId;
        row.PlanName = subscription.PlanName;
        row.Status = subscription.Status;
        row.Price = subscription.Price;
        row.BillingPeriod = subscription.BillingPeriod;
        row.MaxUsers = subscription.MaxUsers;
        row.MaxProductiveUnits = subscription.MaxProductiveUnits;
        row.StartsAt = subscription.StartsAt;
        row.TrialEndsAt = subscription.TrialEndsAt;
        row.RenewsAt = subscription.RenewsAt;
        row.AutoRenew = subscription.AutoRenew;
        row.AddOnModuleIds = subscription.AddOnModuleIds.ToArray();

        var current = await dbContext.TenantModules.Where(m => m.TenantId == tenantId).ToListAsync(cancellationToken).ConfigureAwait(false);
        dbContext.TenantModules.RemoveRange(current.Where(m => !moduleIds.Contains(m.ModuleId)));
        foreach (var moduleId in moduleIds.Where(id => current.All(m => m.ModuleId != id)))
        {
            dbContext.TenantModules.Add(new TenantModuleDbModel { TenantId = tenantId, ModuleId = moduleId, UpdatedAt = clock.GetUtcNow() });
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Logger.Debug("Saved subscription of tenant {TenantId} with {ModuleCount} modules", tenantId, moduleIds.Count);
    }

    public async Task SetModuleStatusAsync(Guid tenantId, string moduleId, string status, string? error, CancellationToken cancellationToken = default)
    {
        var id = tenantId.ToString();
        var module = await dbContext.TenantModules.SingleOrDefaultAsync(m => m.TenantId == id && m.ModuleId == moduleId, cancellationToken).ConfigureAwait(false);
        if (module is null) return;
        if (status == ProvisioningStatuses.Provisioning) module.Attempts++;
        module.Status = status;
        module.LastError = error;
        module.UpdatedAt = clock.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<(Guid TenantId, string ModuleId)>> GetFailedAsync(int maxAttempts, CancellationToken cancellationToken = default)
    {
        var failed = await dbContext.TenantModules.AsNoTracking()
            .Where(m => m.Status == ProvisioningStatuses.Failed && m.Attempts < maxAttempts)
            .Select(m => new { m.TenantId, m.ModuleId })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return failed.Select(m => (Guid.Parse(m.TenantId), m.ModuleId)).ToArray();
    }

    private async Task<Dictionary<Guid, int>> SubscriberCountsAsync(CancellationToken cancellationToken) =>
        await dbContext.Subscriptions.AsNoTracking()
            .GroupBy(s => s.PlanId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken)
            .ConfigureAwait(false);

    private async Task<SubscriptionReadModel[]> ToReadModelsAsync(IReadOnlyCollection<SubscriptionDbModel> rows, CancellationToken cancellationToken)
    {
        var tenantIds = rows.Select(r => r.TenantId).ToArray();
        var names = await dbContext.Tenants.AsNoTracking().Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.CompanyName, cancellationToken).ConfigureAwait(false);
        var tenantModules = await dbContext.TenantModules.AsNoTracking().Where(m => tenantIds.Contains(m.TenantId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var modules = await dbContext.Modules.AsNoTracking().ToDictionaryAsync(m => m.Id, cancellationToken).ConfigureAwait(false);
        return rows.Select(r => new SubscriptionReadModel(
            Guid.Parse(r.TenantId), names.GetValueOrDefault(r.TenantId, r.TenantId), r.PlanId, r.PlanName, r.Status, r.Price, r.BillingPeriod,
            r.MaxUsers, r.MaxProductiveUnits, r.StartsAt, r.TrialEndsAt, r.RenewsAt, r.AutoRenew, r.AddOnModuleIds,
            tenantModules.Where(m => m.TenantId == r.TenantId && modules.ContainsKey(m.ModuleId))
                .OrderByDescending(m => modules[m.ModuleId].IsCore).ThenBy(m => modules[m.ModuleId].Name)
                .Select(m => new TenantModuleReadModel(
                    m.ModuleId, modules[m.ModuleId].Name, modules[m.ModuleId].Icon, modules[m.ModuleId].Version,
                    m.Status, m.Attempts, m.LastError, r.AddOnModuleIds.Contains(m.ModuleId), m.UpdatedAt))
                .ToArray())).ToArray();
    }

    private static ModuleReadModel ToReadModel(ModuleDbModel m, int tenantCount) =>
        new(m.Id, m.Name, m.Description, m.Icon, m.Status, m.Version, m.IsCore, m.Requires, m.Permissions, m.ProvisionUrl, m.UpdatedAt, tenantCount);

    private static PlanReadModel ToReadModel(PlanDbModel p, int subscribers) =>
        new(p.Id, p.Code, p.Name, p.Description, p.Price, p.BillingPeriod, p.TrialDays, p.MaxUsers, p.MaxProductiveUnits, p.ModuleIds, p.Active, subscribers);
}
