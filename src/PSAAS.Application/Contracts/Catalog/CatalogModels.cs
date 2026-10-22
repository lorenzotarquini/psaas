namespace PSAAS.Application.Contracts.Catalog;

public static class ModuleStatuses
{
    public const string Beta = "Beta";
    public const string Available = "Available";
    public const string Deprecated = "Deprecated";

    public static readonly string[] All = [Beta, Available, Deprecated];
}

/// <summary>UC15: what a module declares about itself (ADMIN-PORTAL.md §6.2).</summary>
/// <param name="Id">Stable key, lowercase letters, digits and dashes (e.g. "warehouse").</param>
/// <param name="Version">Semantic version, e.g. "1.4.0".</param>
/// <param name="Requires">Modules it needs (core is always there).</param>
/// <param name="ProvisionUrl">Called when the module is activated for a tenant; none means nothing to prepare.</param>
public sealed record ModuleManifest(
    string? Id,
    string? Name,
    string? Description,
    string? Version,
    string? Icon,
    string? Status,
    IReadOnlyList<string>? Requires,
    IReadOnlyList<string>? Permissions,
    string? ProvisionUrl,
    string? DeprovisionUrl);

public sealed record ModuleReadModel(
    string Id,
    string Name,
    string? Description,
    string? Icon,
    string Status,
    string Version,
    bool IsCore,
    IReadOnlyList<string> Requires,
    IReadOnlyList<string> Permissions,
    string? ProvisionUrl,
    DateTimeOffset UpdatedAt,
    int TenantCount);

public static class BillingPeriods
{
    public const string Monthly = "Monthly";
    public const string Yearly = "Yearly";

    public static readonly string[] All = [Monthly, Yearly];
}

/// <summary>UC16: a commercial plan; MaxUsers/MaxProductiveUnits null = no limit.</summary>
public sealed record PlanRequest(
    string? Code,
    string? Name,
    string? Description,
    decimal? Price,
    string? BillingPeriod,
    int? TrialDays,
    int? MaxUsers,
    int? MaxProductiveUnits,
    IReadOnlyList<string>? ModuleIds,
    bool Active = true);

public sealed record PlanReadModel(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    decimal Price,
    string BillingPeriod,
    int TrialDays,
    int? MaxUsers,
    int? MaxProductiveUnits,
    IReadOnlyList<string> ModuleIds,
    bool Active,
    int SubscriberCount);

public static class SubscriptionStatuses
{
    public const string Trial = "Trial";
    public const string Active = "Active";
    public const string Expired = "Expired";
    public const string Cancelled = "Cancelled";
}

public static class ProvisioningStatuses
{
    public const string Provisioning = "Provisioning";
    public const string Active = "Active";
    public const string Failed = "Failed";
}

/// <summary>UC17: subscribe a tenant to a plan, or change it.</summary>
/// <param name="Trial">Start with the trial days of the plan (only for a first subscription).</param>
/// <param name="AddOnModuleIds">Modules bought on top of the plan.</param>
public sealed record SubscriptionRequest(Guid? PlanId, bool Trial = false, IReadOnlyList<string>? AddOnModuleIds = null, bool AutoRenew = true);

/// <summary>The subscription of a tenant, with the values of the plan frozen when it was subscribed (grandfathering).</summary>
public sealed record SubscriptionReadModel(
    Guid TenantId,
    string TenantName,
    Guid PlanId,
    string PlanName,
    string Status,
    decimal Price,
    string BillingPeriod,
    int? MaxUsers,
    int? MaxProductiveUnits,
    DateTimeOffset StartsAt,
    DateTimeOffset? TrialEndsAt,
    DateTimeOffset RenewsAt,
    bool AutoRenew,
    IReadOnlyList<string> AddOnModuleIds,
    IReadOnlyList<TenantModuleReadModel> Modules);

public sealed record TenantModuleReadModel(
    string ModuleId,
    string Name,
    string? Icon,
    string Version,
    string Status,
    int Attempts,
    string? LastError,
    bool IsAddOn,
    DateTimeOffset UpdatedAt);

/// <summary>A tenant module to (re)provision and how it went.</summary>
public sealed record ProvisioningOutcome(bool Succeeded, string? Error);
