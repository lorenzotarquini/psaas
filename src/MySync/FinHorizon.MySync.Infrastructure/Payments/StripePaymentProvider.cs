using FinHorizon.Modules.Billing;
using FinHorizon.Modules.Sdk;
using FinHorizon.MySync.Application.Abstractions.Payments;
using FinHorizon.MySync.Application.Features.Memberships;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Memberships;
using Microsoft.Extensions.Options;

namespace FinHorizon.MySync.Infrastructure.Payments;

/// <summary>
/// UC17 on Stripe through the shared module billing (FinHorizon.Modules.Billing, docs/architettura-moduli.md): this
/// class only maps its values to MySync's own; the Stripe calls are the same for every module.
/// </summary>
internal sealed class StripePaymentProvider : IPaymentProvider, IDisposable
{
    private readonly StripeModuleBilling _billing;

    public StripePaymentProvider(IOptions<StripeOptions> stripeOptions, IOptions<MembershipOptions> membershipOptions)
    {
        ArgumentNullException.ThrowIfNull(stripeOptions);
        ArgumentNullException.ThrowIfNull(membershipOptions);
        _billing = new StripeModuleBilling(new ModuleBillingOptions
        {
            SecretKey = stripeOptions.Value.SecretKey,
            ModuleId = membershipOptions.Value.ModuleId,
        });
    }

    public Task<string> CreateCustomerAsync(OwnerId ownerId, string? email, string? name, CancellationToken cancellationToken = default) =>
        CallAsync(() => _billing.CreateCustomerAsync(ownerId.Value, email, name, cancellationToken));

    public Task<IReadOnlyList<MembershipPlan>> GetPlansAsync(CancellationToken cancellationToken = default) =>
        CallAsync<IReadOnlyList<MembershipPlan>>(async () => (await _billing.GetPlansAsync(cancellationToken).ConfigureAwait(false))
            .Select(plan => new MembershipPlan(
                plan.Code, plan.Name, plan.Description, plan.Tier, plan.Price, plan.Currency, plan.Interval, plan.PriceIncludesVat, plan.VatRate,
                plan.TrialDays, plan.PriceId, plan.TaxRateId))
            .ToList());

    public Task<Uri> CreateCheckoutAsync(string customerId, OwnerId ownerId, MembershipPlan plan, bool withTrial, Uri returnUrl, CancellationToken cancellationToken = default)
    {
        var billingPlan = ToBilling(plan);
        return CallAsync(() => _billing.CreateCheckoutAsync(customerId, ownerId.Value, billingPlan, withTrial, returnUrl, cancellationToken));
    }

    private static BillingPlan ToBilling(MembershipPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return new BillingPlan(
            plan.Code, plan.Name, plan.Description, plan.Tier, plan.Price, plan.Currency, plan.Interval, plan.PriceIncludesVat, plan.VatRate,
            plan.TrialDays, plan.PriceId, plan.TaxRateId);
    }

    public Task<PlanChangeOutcome> ChangePlanAsync(string customerId, string subscriptionId, MembershipPlan plan, CancellationToken cancellationToken = default) =>
        CallAsync(async () =>
        {
            var change = await _billing.ChangePlanAsync(customerId, subscriptionId, ToBilling(plan), cancellationToken).ConfigureAwait(false);
            return new PlanChangeOutcome(change.Applied, change.Failure is { } failure ? new PaymentFailure(failure.Code, failure.Message, failure.FailedAt) : null);
        });

    public Task<Uri> CreatePortalAsync(string customerId, Uri returnUrl, CancellationToken cancellationToken = default) =>
        CallAsync(() => _billing.CreatePortalAsync(customerId, returnUrl, cancellationToken));

    public Task<MembershipSnapshot> GetSnapshotAsync(string customerId, CancellationToken cancellationToken = default) =>
        CallAsync(async () =>
        {
            var subscription = await _billing.GetSubscriptionAsync(customerId, cancellationToken).ConfigureAwait(false);
            return subscription.SubscriptionId is null
                ? MembershipSnapshot.None
                : new MembershipSnapshot(
                    StatusOf(subscription.Status),
                    subscription.SubscriptionId,
                    subscription.CurrentPeriodEnd,
                    subscription.CancelAtPeriodEnd,
                    subscription.Card is { } card ? new PaymentCard(card.Brand, card.Last4, card.ExpMonth, card.ExpYear) : null,
                    subscription.LastFailure is { } failure ? new PaymentFailure(failure.Code, failure.Message, failure.FailedAt) : null,
                    subscription.PayUrl,
                    subscription.Plan is { } plan ? new SubscribedPlan(plan.Name, plan.Amount, plan.Currency, plan.Interval, plan.Tier) : null);
        });

    public void Dispose() => _billing.Dispose();

    /// <summary>UC17-R3, from the contract statuses (C3).</summary>
    internal static MembershipStatus StatusOf(string stripeStatus) => StatusOf(StripeModuleBilling.StatusOf(stripeStatus));

    private static MembershipStatus StatusOf(EntitlementStatus status) => status switch
    {
        EntitlementStatus.Active => MembershipStatus.Active,
        EntitlementStatus.PastDue => MembershipStatus.PastDue,
        _ => MembershipStatus.Expired,
    };

    private static async Task<T> CallAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call().ConfigureAwait(false);
        }
        catch (BillingUnavailableException exception)
        {
            throw new PaymentProviderUnavailableException("Stripe is unavailable.", exception);
        }
    }
}
