using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Memberships;

/// <summary>UC17-R3: the three states the user sees; Stripe has more, mapped by the payment provider.</summary>
public enum MembershipStatus
{
    Expired = 0,
    Active = 1,
    PastDue = 2,
}

/// <summary>The card the subscription is paid with, as Stripe shows it (never the full number).</summary>
public sealed record PaymentCard(string Brand, string Last4, int ExpMonth, int ExpYear);

/// <summary>The last payment that did not go through: Stripe's code (e.g. card_declined) and message.</summary>
public sealed record PaymentFailure(string? Code, string? Message, DateTimeOffset FailedAt);

/// <summary>What Stripe says now about a customer: the copy of <see cref="Membership"/> is replaced by it (UC17-R5).</summary>
public sealed record MembershipSnapshot(
    MembershipStatus Status,
    string? SubscriptionId,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    PaymentCard? Card,
    PaymentFailure? LastFailure,
    Uri? PayUrl)
{
    public static readonly MembershipSnapshot None = new(MembershipStatus.Expired, null, null, false, null, null, null);
}

/// <summary>
/// UC17: the subscription of a user to MySync, a copy of what Stripe decides. One per user, created with the Stripe
/// customer at the first payment (R2); no row means never paid, so Expired (R3).
/// </summary>
public sealed class Membership
{
    public Membership(OwnerId ownerId, string customerId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        OwnerId = ownerId;
        CustomerId = customerId;
        UpdatedAt = now;
    }

    /// <summary>For materialization by the persistence layer only.</summary>
    private Membership()
    {
    }

    public OwnerId OwnerId { get; private set; }

    public string CustomerId { get; private set; } = string.Empty;

    public MembershipStatus Status { get; private set; }

    public string? SubscriptionId { get; private set; }

    public DateTimeOffset? CurrentPeriodEnd { get; private set; }

    public bool CancelAtPeriodEnd { get; private set; }

    public PaymentCard? Card { get; private set; }

    public PaymentFailure? LastFailure { get; private set; }

    public Uri? PayUrl { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>UC17-R4: read-only when expired; while Stripe retries (PastDue) the user still writes.</summary>
    public bool CanWrite => Status is MembershipStatus.Active or MembershipStatus.PastDue;

    /// <summary>A subscription that still runs or waits for its payment: paying again would charge twice (UC17 flow 2b).</summary>
    public bool IsOpen => Status is MembershipStatus.Active or MembershipStatus.PastDue;

    public void Apply(MembershipSnapshot snapshot, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Status = snapshot.Status;
        SubscriptionId = snapshot.SubscriptionId;
        CurrentPeriodEnd = snapshot.CurrentPeriodEnd;
        CancelAtPeriodEnd = snapshot.CancelAtPeriodEnd;
        Card = snapshot.Card;
        LastFailure = snapshot.LastFailure;
        PayUrl = snapshot.PayUrl;
        UpdatedAt = now;
    }
}
