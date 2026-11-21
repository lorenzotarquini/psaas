using FinHorizon.Modules.Sdk;

namespace FinHorizon.Modules.Billing;

/// <summary>Stripe keys and the module whose plans are sold (docs/architettura-moduli.md).</summary>
public sealed class ModuleBillingOptions
{
    /// <summary>sk_test_… / sk_live_…; empty = every call throws <see cref="BillingUnavailableException"/>.</summary>
    public string? SecretKey { get; set; }

    /// <summary>whsec_… of the webhook endpoint (or of the Stripe CLI locally); empty = webhooks refused.</summary>
    public string? WebhookSecret { get; set; }

    /// <summary>The module id of the catalog, e.g. "mysync": its plans carry it in metadata.module.</summary>
    public string ModuleId { get; set; } = string.Empty;
}

/// <summary>
/// A plan of the module on sale, as the Admin Portal publishes it (UC16): a product with metadata module, plan_code,
/// tier, tax_rate and trial_days, and its default price.
/// </summary>
public sealed record BillingPlan(
    string Code,
    string Name,
    string? Description,
    int? Tier,
    decimal Price,
    string Currency,
    string Interval,
    bool PriceIncludesVat,
    decimal? VatRate,
    int TrialDays,
    string PriceId,
    string? TaxRateId);

/// <summary>The card a subscription is paid with, as Stripe shows it (never the full number).</summary>
public sealed record BillingCard(string Brand, string Last4, int ExpMonth, int ExpYear);

/// <summary>The last payment that did not go through: Stripe's decline or error code and its message.</summary>
public sealed record BillingFailure(string? Code, string? Message, DateTimeOffset FailedAt);

/// <summary>The plan a subscription pays, as when the user subscribed.</summary>
public sealed record BillingSubscribedPlan(string Name, int? Tier, decimal Amount, string Currency, string Interval);

/// <summary>Contract C3: the subscription of a customer to the module, as Stripe says now.</summary>
public sealed record BillingSubscription(
    EntitlementStatus Status,
    string? SubscriptionId,
    BillingSubscribedPlan? Plan,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    BillingCard? Card,
    BillingFailure? LastFailure,
    Uri? PayUrl)
{
    public static readonly BillingSubscription None = new(EntitlementStatus.None, null, null, null, false, null, null, null);
}

/// <summary>The outcome of a plan change: applied, or refused by the payment of the difference (with its failure).</summary>
public sealed record BillingPlanChange(bool Applied, BillingFailure? Failure);

/// <summary>A verified Stripe event: only the customer it concerns, the rest is read again from Stripe (C7).</summary>
public sealed record BillingNotification(string EventId, string Type, string? CustomerId);

/// <summary>Stripe cannot be reached or is not configured: the request cannot be answered now (503).</summary>
public sealed class BillingUnavailableException : Exception
{
    public BillingUnavailableException()
    {
    }

    public BillingUnavailableException(string message)
        : base(message)
    {
    }

    public BillingUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
