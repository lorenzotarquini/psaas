namespace FinHorizon.AdminPortal.Application.Contracts.Catalog;

public sealed record ModuleReadModel(
    string Id,
    string Name,
    string? Description,
    string? Icon,
    string Status,
    string Version,
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

/// <summary>
/// UC16: a commercial plan for one module (R2); MaxUsers/MaxProductiveUnits null = no limit. VAT: rate 22/10/5/4/0
/// (default 22), the nature (N1–N7) only with 0% (R4).
/// </summary>
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
    bool Active = true,
    decimal? VatRate = null,
    string? VatNature = null,
    bool PriceIncludesVat = true);

/// <summary>UC16-R5: where a plan lives on Stripe; null on a plan not published yet.</summary>
public sealed record PlanStripeLink(string ProductId, string PriceId, string TaxRateId);

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
    int SubscriberCount,
    decimal VatRate = 22,
    string? VatNature = null,
    bool PriceIncludesVat = true,
    PlanStripeLink? Stripe = null)
{
    /// <summary>UC16-2c: on sale on Stripe.</summary>
    public bool Published => Stripe is not null;
}

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
