using NLog;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Application.Contracts.Catalog;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Domain;

namespace PSAAS.Application.Services.Tenants;

/// <summary>
/// UC13 (EXP-04): moves a tenant through its statuses, only along the allowed transitions, with a reason
/// when suspending, giving notice or ceasing. UC14: activates a draft or discards it.
/// </summary>
public sealed class TenantLifecycleService(
    ITenantReadRepository reader,
    ITenantWriteRepository tenants,
    IAuditLog audit,
    TimeProvider clock,
    ISubscriptionRepository subscriptions)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<TenantEditResult> ChangeStatusAsync(
        Guid tenantId,
        TenantStatusChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenant = await reader.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return TenantEditResult.NotFound;
        }

        var errors = new Dictionary<string, string>();
        if (!Enum.TryParse<TenantStatus>(request.Status, ignoreCase: true, out var next) || !tenant.Lifecycle.CanChangeTo(next))
        {
            errors["status"] = TenantFieldValidationErrorCodes.InvalidValue;
        }
        else if (TenantLifecycle.RequiresReason(next) && string.IsNullOrWhiteSpace(request.Reason))
        {
            errors["reason"] = TenantFieldValidationErrorCodes.Required;
        }

        if (errors.Count > 0)
        {
            Logger.Info("Status change of tenant {TenantId} from {From} to {To} rejected: {ValidationErrors}", tenantId, tenant.Lifecycle.Status, request.Status, errors);
            return new TenantEditResult(true, errors);
        }

        // UC14 + EXP-03: a draft activated on a trial subscription starts in Trial.
        if (next == TenantStatus.Active && tenant.Lifecycle.Status == TenantStatus.Onboarding
            && (await subscriptions.GetAsync(tenantId, cancellationToken))?.Status == SubscriptionStatuses.Trial)
        {
            next = TenantStatus.Trial;
        }

        var from = tenant.Lifecycle.Status;
        tenant.ChangeStatus(next, request.Reason, clock.GetUtcNow());
        if (!await tenants.UpdateLifecycleAsync(tenantId, tenant.Lifecycle, cancellationToken))
        {
            return TenantEditResult.NotFound;
        }

        await audit.RecordAsync(
            new AuditRecord(
                AuditActions.TenantStatusChanged,
                AuditTargets.Tenant,
                tenantId.ToString(),
                tenant.CompanyName,
                tenantId.ToString(),
                new { from = from.ToString(), to = next.ToString(), reason = tenant.Lifecycle.Reason }),
            cancellationToken);
        Logger.Info("Tenant {TenantId} went from {From} to {To}", tenantId, from, next);
        return TenantEditResult.Ok;
    }

    /// <summary>UC14: a draft (Onboarding) that is abandoned disappears; any other tenant cannot be deleted.</summary>
    public async Task<DiscardResult> DiscardDraftAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await reader.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return DiscardResult.NotFound;
        }

        if (tenant.Lifecycle.Status != TenantStatus.Onboarding)
        {
            Logger.Info("Tenant {TenantId} is {Status}: only drafts can be discarded", tenantId, tenant.Lifecycle.Status);
            return DiscardResult.NotADraft;
        }

        await tenants.DeleteAsync(tenantId, cancellationToken);
        await audit.RecordAsync(
            new AuditRecord(AuditActions.TenantOnboardingDiscarded, AuditTargets.Tenant, tenantId.ToString(), tenant.CompanyName, tenantId.ToString()),
            cancellationToken);
        return DiscardResult.Discarded;
    }
}

public enum DiscardResult
{
    Discarded,
    NotFound,
    NotADraft,
}
