using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Features.Currencies;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.Subscriptions;

namespace FinHorizon.MySync.Application.Features.Subscriptions;

/// <summary>
/// A subscription as the user sees it (UC16 step 2): <see cref="DueOn"/> is the charge to register (R7), <see cref="NextChargeOn"/>
/// the first one after today, <see cref="DisplayAmount"/> the amount in the display currency with today's rate.
/// </summary>
public sealed record SubscriptionView(
    Guid Id,
    string Name,
    string? Description,
    Guid AccountId,
    string AccountName,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    string Currency,
    decimal DisplayAmount,
    DateOnly FirstChargeOn,
    DateOnly? EndsOn,
    DateOnly CreatedOn,
    bool Active,
    DateOnly? NextChargeOn,
    DateOnly? DueOn,
    bool HasPayments);

/// <summary>UC16 step 2: the subscriptions, the monthly total of the active ones (R16) and the limit (R4).</summary>
public sealed record SubscriptionList(IReadOnlyList<SubscriptionView> Items, decimal MonthlyTotal, string Currency, int Max);

/// <summary>Builds the views of the user's subscriptions with the names they point to, their payments and today's rates.</summary>
public sealed class SubscriptionReader(
    ISubscriptionRepository subscriptions,
    IAccountRepository accounts,
    ICategoryRepository categories,
    ILedgerReader ledger,
    CurrencyConverter converter,
    IResourceLimits limits,
    ICalendar calendar)
{
    public async Task<SubscriptionList> ListAsync(OwnerId owner, CancellationToken cancellationToken = default)
    {
        var all = await subscriptions.ListAsync(owner, cancellationToken).ConfigureAwait(false);
        var views = await ViewsAsync(owner, all, cancellationToken).ConfigureAwait(false);
        var max = (await limits.ForAsync(owner, cancellationToken).ConfigureAwait(false)).MaxSubscriptions;
        var display = await converter.DisplayCurrencyAsync(owner, cancellationToken).ConfigureAwait(false);
        var ordered = views
            .OrderBy(view => view.EndsOn is not null)
            .ThenByDescending(view => view.EndsOn)
            .ThenBy(view => view.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new SubscriptionList(ordered, views.Where(view => view.Active).Sum(view => view.DisplayAmount), display, max);
    }

    public async Task<SubscriptionView> ViewAsync(OwnerId owner, Subscription subscription, CancellationToken cancellationToken = default) =>
        (await ViewsAsync(owner, [subscription], cancellationToken).ConfigureAwait(false)).Single();

    private async Task<IReadOnlyList<SubscriptionView>> ViewsAsync(OwnerId owner, IReadOnlyList<Subscription> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var today = calendar.Today;
        var display = await converter.DisplayCurrencyAsync(owner, cancellationToken).ConfigureAwait(false);
        var rates = await converter.RatesAsync(display, items.Select(item => item.Currency), today, today, cancellationToken).ConfigureAwait(false);
        var accountNames = (await accounts.ListAsync(owner, cancellationToken).ConfigureAwait(false)).ToDictionary(account => account.Id, account => account.Descriptor.Name);
        var payments = (await ledger.SubscriptionPaymentsAsync(owner, items.Count == 1 ? items[0].Id : null, cancellationToken).ConfigureAwait(false))
            .ToLookup(payment => payment.SubscriptionId, payment => payment.Date);
        var views = new List<SubscriptionView>();
        foreach (var item in items)
        {
            var category = await categories.GetAsync(owner, item.CategoryId, cancellationToken).ConfigureAwait(false);
            var paid = payments[item.Id].ToList();
            views.Add(new SubscriptionView(
                item.Id,
                item.Descriptor.Name,
                item.Descriptor.Description,
                item.AccountId,
                accountNames.GetValueOrDefault(item.AccountId, string.Empty),
                item.CategoryId,
                category?.Name ?? string.Empty,
                item.Amount.Amount,
                item.Currency,
                RateTable.Round(rates.ToDisplay(item.Currency, item.Amount.Amount, today)),
                item.FirstChargeOn,
                item.EndsOn,
                item.CreatedOn,
                item.IsActiveOn(today),
                item.NextChargeAfter(today),
                item.DueCharge(today, paid),
                paid.Count > 0));
        }

        return views;
    }
}
