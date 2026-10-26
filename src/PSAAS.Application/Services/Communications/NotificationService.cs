using NLog;
using PSAAS.Application.Abstractions.Billing;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Billing;
using PSAAS.Application.Contracts.Catalog;
using PSAAS.Application.Contracts.Communications;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Domain;

namespace PSAAS.Application.Services.Communications;

/// <summary>UC28: the "Da gestire" queue, computed on request so a solved item simply disappears (R2).</summary>
public sealed class NotificationService(
    ISubscriptionRepository subscriptions,
    IInvoiceRepository invoices,
    ITenantListRepository tenantList,
    ITenantReadRepository tenants,
    IUserDirectory users,
    TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const int TrialEndingDays = 7;
    public const int DraftStalledDays = 7;
    public const double UserLimitRatio = 0.9;

    private static readonly string[] Severity = [NotificationSeverities.Critical, NotificationSeverities.Warning, NotificationSeverities.Info];

    public async Task<IReadOnlyList<Notification>> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        var items = new List<Notification>();
        var all = await subscriptions.GetAllAsync(cancellationToken);
        foreach (var subscription in all)
        {
            items.AddRange(subscription.Modules
                .Where(m => m.Status == ProvisioningStatuses.Failed)
                .Select(m => new Notification(NotificationKinds.ModuleFailed, NotificationSeverities.Critical, subscription.TenantId, subscription.TenantName, m.Name, m.ModuleId, m.UpdatedAt)));
            if (subscription.Status == SubscriptionStatuses.Trial && subscription.TrialEndsAt is { } end && end <= now.AddDays(TrialEndingDays))
            {
                items.Add(new Notification(NotificationKinds.TrialEnding, NotificationSeverities.Warning, subscription.TenantId, subscription.TenantName,
                    subscription.PlanName, null, end, Value: Math.Max(0, (int)Math.Ceiling((end - now).TotalDays))));
            }
        }

        items.AddRange((await invoices.GetOpenAsync(cancellationToken))
            .Where(i => i.Status == InvoiceStatuses.Overdue)
            .Select(i => new Notification(NotificationKinds.InvoiceOverdue, NotificationSeverities.Critical, i.TenantId, i.TenantName, i.Number, i.Id.ToString(), i.DueAt)));

        items.AddRange(await UserLimitsAsync(all, now, cancellationToken));

        var drafts = await tenantList.GetListAsync(new TenantListQuery(Statuses: [nameof(TenantStatus.Onboarding)], PageSize: 100), now, cancellationToken);
        items.AddRange(drafts.Items
            .Where(t => t.CreatedAt <= now.AddDays(-DraftStalledDays))
            .Select(t => new Notification(NotificationKinds.DraftStalled, NotificationSeverities.Info, t.Id, t.CompanyName, null, null, t.CreatedAt,
                Value: (int)(now - t.CreatedAt).TotalDays)));

        Logger.Debug("{Count} items to handle", items.Count);
        // R3: most severe first, then the oldest.
        return items.OrderBy(n => Array.IndexOf(Severity, n.Severity)).ThenBy(n => n.At).ToArray();
    }

    // EXP-02: 90% of the plan's users; skipped while Keycloak is unreachable.
    private async Task<IEnumerable<Notification>> UserLimitsAsync(IReadOnlyList<SubscriptionReadModel> all, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var items = new List<Notification>();
        try
        {
            // shortcut: one Keycloak count per limited tenant; cache the counts when tenants are many.
            foreach (var subscription in all.Where(s => s.MaxUsers is > 0 && s.Status is SubscriptionStatuses.Active or SubscriptionStatuses.Trial))
            {
                var profiles = (await tenants.GetProfilesAndProductiveUnitsAsync(subscription.TenantId.ToString(), cancellationToken)).Profiles.Select(p => p.Id).ToArray();
                if (profiles.Length == 0) continue;
                var count = await users.CountUsersWithAnyAsync(profiles, cancellationToken);
                if (count >= subscription.MaxUsers!.Value * UserLimitRatio)
                {
                    items.Add(new Notification(NotificationKinds.UserLimit, NotificationSeverities.Warning, subscription.TenantId, subscription.TenantName,
                        subscription.PlanName, null, now, count, subscription.MaxUsers));
                }
            }
        }
        catch (UserDirectoryUnavailableException exception)
        {
            Logger.Warn(exception, "User limits left out of the queue: Keycloak unreachable");
        }

        return items;
    }
}
