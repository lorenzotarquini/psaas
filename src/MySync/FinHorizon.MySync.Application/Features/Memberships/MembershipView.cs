using FinHorizon.MySync.Domain.Memberships;

namespace FinHorizon.MySync.Application.Features.Memberships;

public sealed record PaymentFailureView(string? Code, string? Message, DateTimeOffset FailedAt);

/// <summary>UC17 step 1: what the settings show; the plan is the one of the current or last subscription.</summary>
public sealed record MembershipView(
    MembershipStatus Status,
    bool CanWrite,
    SubscribedPlan? Plan,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    PaymentCard? Card,
    PaymentFailureView? LastFailure,
    Uri? PayUrl,
    bool HasCustomer)
{
    public static MembershipView From(Membership? membership)
    {
        var failure = membership?.LastFailure;
        return new MembershipView(
            membership?.Status ?? MembershipStatus.Expired,
            membership?.CanWrite ?? false,
            membership?.Plan,
            membership?.CurrentPeriodEnd,
            membership?.CancelAtPeriodEnd ?? false,
            membership?.Card,
            failure is null ? null : new PaymentFailureView(failure.Code, failure.Message, failure.FailedAt),
            membership?.PayUrl,
            membership is not null);
    }
}

/// <summary>UC17 step 2: a plan the user can choose.</summary>
public sealed record MembershipPlanView(
    string Code, string Name, string? Description, decimal Price, string Currency, string Interval, bool PriceIncludesVat, decimal? VatRate, int TrialDays);

/// <summary>A page of Stripe to send the browser to.</summary>
public sealed record RedirectView(Uri Url);
