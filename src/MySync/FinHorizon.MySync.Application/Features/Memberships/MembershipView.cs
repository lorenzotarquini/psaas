using FinHorizon.MySync.Domain.Memberships;

namespace FinHorizon.MySync.Application.Features.Memberships;

public sealed record PaymentFailureView(string? Code, string? Message, DateTimeOffset FailedAt);

/// <summary>UC17 step 1: what the settings show.</summary>
public sealed record MembershipView(
    MembershipStatus Status,
    bool CanWrite,
    decimal Price,
    string Currency,
    string Interval,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    PaymentCard? Card,
    PaymentFailureView? LastFailure,
    Uri? PayUrl,
    bool HasCustomer)
{
    public static MembershipView From(Membership? membership, MembershipOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failure = membership?.LastFailure;
        return new MembershipView(
            membership?.Status ?? MembershipStatus.Expired,
            membership?.CanWrite ?? false,
            options.Price,
            options.Currency,
            "Month",
            membership?.CurrentPeriodEnd,
            membership?.CancelAtPeriodEnd ?? false,
            membership?.Card,
            failure is null ? null : new PaymentFailureView(failure.Code, failure.Message, failure.FailedAt),
            membership?.PayUrl,
            membership is not null);
    }
}

/// <summary>A page of Stripe to send the browser to.</summary>
public sealed record RedirectView(Uri Url);
