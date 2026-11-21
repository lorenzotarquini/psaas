using FinHorizon.Modules.Sdk;
using FinHorizon.MySync.Domain.Memberships;

namespace FinHorizon.MySync.Application.Features.Memberships;

public sealed record PaymentFailureView(string? Code, string? Message, DateTimeOffset FailedAt);

/// <summary>Contract C2: a tier as the user sees it — name, features and limits.</summary>
public sealed record TierView(int Level, string Name, IReadOnlyList<string> Features, IReadOnlyDictionary<string, int> Limits)
{
    public static TierView? From(ModuleTier? tier) =>
        tier is null ? null : new TierView(tier.Level, tier.Name ?? string.Empty, tier.Features ?? [], tier.Limits ?? new Dictionary<string, int>());
}

/// <summary>UC17 step 1: what the settings show; the plan is the one of the current or last subscription, the tier the one paid.</summary>
public sealed record MembershipView(
    MembershipStatus Status,
    bool CanWrite,
    SubscribedPlan? Plan,
    TierView? Tier,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    PaymentCard? Card,
    PaymentFailureView? LastFailure,
    Uri? PayUrl,
    bool HasCustomer)
{
    public static MembershipView From(Membership? membership, ModuleEntitlement entitlement)
    {
        ArgumentNullException.ThrowIfNull(entitlement);
        var failure = membership?.LastFailure;
        return new MembershipView(
            membership?.Status ?? MembershipStatus.Expired,
            entitlement.CanWrite,
            membership?.Plan,
            TierView.From(entitlement.Tier),
            membership?.CurrentPeriodEnd,
            membership?.CancelAtPeriodEnd ?? false,
            membership?.Card,
            failure is null ? null : new PaymentFailureView(failure.Code, failure.Message, failure.FailedAt),
            membership?.PayUrl,
            membership is not null);
    }
}

/// <summary>UC17 step 2: a plan the user can choose; <paramref name="CanUpgradeTo"/>: the running subscription can move to it (R10).</summary>
public sealed record MembershipPlanView(
    string Code, string Name, string? Description, TierView? Tier, decimal Price, string Currency, string Interval, bool PriceIncludesVat, decimal? VatRate, int TrialDays,
    bool CanUpgradeTo = false);

/// <summary>A page of Stripe to send the browser to.</summary>
public sealed record RedirectView(Uri Url);
