using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;

namespace FinHorizon.MySync.Domain.Subscriptions;

/// <summary>
/// UC16: the same expense every month, from one account and in one category, until the user cancels it.
/// It never records movements on its own (D19): every charge is an expense of UC5 linked to it.
/// </summary>
public sealed class Subscription : Entity
{
    private Subscription(Guid id, OwnerId ownerId, DateOnly createdOn)
        : base(id, ownerId)
    {
        CreatedOn = createdOn;
        Descriptor = null!;
        NormalizedName = null!;
        Currency = null!;
    }

    /// <summary>For materialization by the persistence layer only.</summary>
    private Subscription()
    {
        Descriptor = null!;
        NormalizedName = null!;
        Currency = null!;
    }

    /// <summary>Name and description (UC16-R1); a subscription has no tag.</summary>
    public Descriptor Descriptor { get; private set; }

    /// <summary>Unique per user, cancelled subscriptions included (UC16-R1).</summary>
    public string NormalizedName { get; private set; }

    public Guid AccountId { get; private set; }

    /// <summary>A traditional currency, the one of the account (UC16-R2).</summary>
    public string Currency { get; private set; }

    /// <summary>An expense category of the user (UC16-R3).</summary>
    public Guid CategoryId { get; private set; }

    /// <summary>What it costs every month, in <see cref="Currency"/>.</summary>
    public Money Amount { get; private set; }

    /// <summary>The first charge: its day of the month is the day of every charge (UC16-R6).</summary>
    public DateOnly FirstChargeOn { get; private set; }

    /// <summary>The last day of the subscription, when cancelled (UC16-R13).</summary>
    public DateOnly? EndsOn { get; private set; }

    /// <summary>Charges before this day are never due (UC16-R7).</summary>
    public DateOnly CreatedOn { get; private set; }

    /// <summary>The charge the user skipped last (UC16-R9).</summary>
    public DateOnly? SkippedChargeOn { get; private set; }

    /// <summary>Cancelled, even if still active until <see cref="EndsOn"/>: it no longer counts in the limit (UC16-R4).</summary>
    public bool IsCancelled => EndsOn is not null;

    public static Subscription Start(
        OwnerId ownerId,
        Descriptor descriptor,
        Guid accountId,
        string currency,
        Guid categoryId,
        Money amount,
        DateOnly firstChargeOn,
        DateOnly today)
    {
        var subscription = new Subscription(Guid.NewGuid(), ownerId, today);
        subscription.Update(descriptor, accountId, currency, categoryId, amount, firstChargeOn);
        return subscription;
    }

    /// <summary>UC16-R12: everything but the creation day; the use case checks the linked expenses first.</summary>
    public void Update(Descriptor descriptor, Guid accountId, string currency, Guid categoryId, Money amount, DateOnly firstChargeOn)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (descriptor.Tag is not null)
        {
            throw new ArgumentException("A subscription has no tag.", nameof(descriptor));
        }

        if (!amount.IsPositive || !Money.HasValidPrecision(amount.Amount))
        {
            throw new ArgumentException("The amount must be a positive amount in cents.", nameof(amount));
        }

        if (!CurrencyRules.IsWellFormed(currency))
        {
            throw new ArgumentException("Not a currency code.", nameof(currency));
        }

        if (EndsOn is { } end && end < firstChargeOn)
        {
            throw new InvalidOperationException("The first charge cannot follow the end.");
        }

        Descriptor = descriptor;
        NormalizedName = descriptor.NormalizedName;
        AccountId = accountId;
        Currency = currency;
        CategoryId = categoryId;
        Amount = amount;
        FirstChargeOn = firstChargeOn;
    }

    /// <summary>UC16-R13: the use case checks the last linked expense; the end can be in the future.</summary>
    public void Cancel(DateOnly endsOn)
    {
        if (endsOn < FirstChargeOn)
        {
            throw new ArgumentOutOfRangeException(nameof(endsOn), "The end cannot precede the first charge.");
        }

        EndsOn = endsOn;
    }

    /// <summary>UC16-R14: the charges fallen while it was cancelled are never due, so the latest of them is skipped.</summary>
    public void Resume(DateOnly today)
    {
        if (EndsOn is not { } end)
        {
            return;
        }

        EndsOn = null;
        if (LastChargeOnOrBefore(today) is { } charge && charge > end)
        {
            SkippedChargeOn = charge;
        }
    }

    /// <summary>UC16-R9: only the charge due right now can be skipped.</summary>
    public void Skip(DateOnly chargeOn) => SkippedChargeOn = chargeOn;

    /// <summary>Active on a day: not ended before it (UC16-R13).</summary>
    public bool IsActiveOn(DateOnly day) => EndsOn is null || EndsOn >= day;

    /// <summary>UC16-R6: the charge in a calendar month, on the day of the first charge or the last of the month.</summary>
    public DateOnly ChargeIn(int year, int month) =>
        new(year, month, Math.Min(FirstChargeOn.Day, DateTime.DaysInMonth(year, month)));

    /// <summary>Whether a charge falls between the first charge and the end (UC16-R6, UC14-R13).</summary>
    public bool Charges(DateOnly charge) => charge >= FirstChargeOn && (EndsOn is null || charge <= EndsOn);

    /// <summary>The most recent charge up to <paramref name="day"/>, both included; null before the first charge.</summary>
    public DateOnly? LastChargeOnOrBefore(DateOnly day)
    {
        var limit = EndsOn is { } end && end < day ? end : day;
        var charge = ChargeIn(limit.Year, limit.Month);
        if (charge > limit)
        {
            var previous = limit.AddMonths(-1);
            charge = ChargeIn(previous.Year, previous.Month);
        }

        return charge >= FirstChargeOn ? charge : null;
    }

    /// <summary>The first charge after <paramref name="day"/>; null when the subscription ends before it.</summary>
    public DateOnly? NextChargeAfter(DateOnly day)
    {
        DateOnly charge;
        if (day < FirstChargeOn)
        {
            charge = FirstChargeOn;
        }
        else
        {
            charge = ChargeIn(day.Year, day.Month);
            if (charge <= day)
            {
                var next = day.AddMonths(1);
                charge = ChargeIn(next.Year, next.Month);
            }
        }

        return Charges(charge) ? charge : null;
    }

    /// <summary>
    /// UC16-R7: the charge to register, if any: the most recent one up to today, not before the creation, with no linked
    /// expense in its calendar month and not skipped.
    /// </summary>
    public DateOnly? DueCharge(DateOnly today, IEnumerable<DateOnly> paymentDates)
    {
        ArgumentNullException.ThrowIfNull(paymentDates);
        if (LastChargeOnOrBefore(today) is not { } charge || charge < CreatedOn || charge == SkippedChargeOn)
        {
            return null;
        }

        return paymentDates.Any(date => date.Year == charge.Year && date.Month == charge.Month) ? null : charge;
    }
}
