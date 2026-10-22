using System.Text.Json;

namespace PSAAS.Application.Contracts.Audit;

/// <summary>Action codes of the audit log; the portal turns each into a sentence.</summary>
public static class AuditActions
{
    public const string TenantCreated = "tenant.created";
    public const string TenantRegistryUpdated = "tenant.registry.updated";
    public const string TenantOrganizationUpdated = "tenant.organization.updated";
    public const string TenantOnboardingStarted = "tenant.onboarding.started";
    public const string TenantOnboardingDiscarded = "tenant.onboarding.discarded";
    public const string TenantStatusChanged = "tenant.status.changed";
    public const string UserCreated = "user.created";
    public const string ModuleRegistered = "module.registered";
    public const string ModuleUpdated = "module.updated";
    public const string PlanCreated = "plan.created";
    public const string PlanUpdated = "plan.updated";
    public const string SubscriptionChanged = "subscription.changed";
    public const string ModuleProvisioned = "module.provisioned";
    public const string ModuleProvisioningFailed = "module.provisioning.failed";
}

public static class AuditTargets
{
    public const string Tenant = "tenant";
    public const string User = "user";
    public const string Module = "module";
    public const string Plan = "plan";
}

/// <summary>What happened, without the actor (added by the audit log from the current request).</summary>
/// <param name="TargetName">Display name at the time of the event (a later rename does not change history).</param>
/// <param name="TenantId">The tenant the event belongs to, for its history; null for platform-wide events.</param>
/// <param name="Details">Action specific data, serialized as JSON (e.g. changed fields with before and after).</param>
public sealed record AuditRecord(
    string Action,
    string TargetType,
    string TargetId,
    string TargetName,
    string? TenantId,
    object? Details = null);

public sealed record AuditEventReadModel(
    Guid Id,
    DateTimeOffset OccurredAt,
    string ActorId,
    string ActorName,
    string Action,
    string TargetType,
    string TargetId,
    string TargetName,
    string? TenantId,
    JsonElement? Details);

/// <param name="Action">An action code, or its prefix (e.g. "tenant." for every tenant event).</param>
/// <param name="Search">Text in the actor or target name, ignoring case.</param>
public sealed record AuditQuery(
    string? TenantId = null,
    string? Action = null,
    string? Search = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 50);

/// <summary>A field changed by an edit (UC4): what it was and what it is now.</summary>
public sealed record AuditChange(string? Before, string? After);

/// <summary>Details of tenant.registry.updated: only the fields that changed.</summary>
public sealed record RegistryChanges(IReadOnlyDictionary<string, AuditChange> Changes);
