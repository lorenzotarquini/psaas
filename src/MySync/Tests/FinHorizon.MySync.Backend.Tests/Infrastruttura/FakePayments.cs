using FinHorizon.MySync.Application.Abstractions.Payments;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Memberships;

namespace FinHorizon.MySync.Backend.Tests.Infrastruttura;

/// <summary>
/// Stripe simulated (UC17): scenarios decide what Stripe "says" about each customer and look at what MySync asked.
/// </summary>
public sealed class FakePayments : IPaymentProvider
{
    public List<(OwnerId Owner, string? Email, string? Name, string CustomerId)> Customers { get; } = [];

    public List<(string CustomerId, string PlanCode, bool WithTrial, Uri ReturnUrl)> Checkouts { get; } = [];

    /// <summary>The plans of the MySync module on Stripe (UC16 of the Admin Portal): monthly 4.99 with 14 days of trial.</summary>
    public List<MembershipPlan> Plans { get; } =
    [
        new("mysync-mensile", "MySync mensile", null, 2, 4.99m, "EUR", "Month", true, 22m, 14, "price_monthly", "txr_22"),
        new("mysync-annuale", "MySync annuale", "Due mesi gratis", 3, 49m, "EUR", "Year", true, 22m, 0, "price_yearly", "txr_22"),
        // A plan for a level MySync does not declare: never offered nor sold (contract C3).
        new("mysync-ignoto", "Livello sconosciuto", null, 4, 99m, "EUR", "Month", true, 22m, 0, "price_unknown", "txr_22"),
    ];

    public List<(string CustomerId, Uri ReturnUrl)> Portals { get; } = [];

    /// <summary>What Stripe answers for a customer; unknown customers have no subscription.</summary>
    public Dictionary<string, MembershipSnapshot> Snapshots { get; } = [];

    public int SnapshotCalls { get; private set; }

    public Task<string> CreateCustomerAsync(OwnerId ownerId, string? email, string? name, CancellationToken cancellationToken = default)
    {
        var customerId = $"cus_{ownerId.Value}";
        Customers.Add((ownerId, email, name, customerId));
        return Task.FromResult(customerId);
    }

    public Task<IReadOnlyList<MembershipPlan>> GetPlansAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MembershipPlan>>(Plans);

    public Task<Uri> CreateCheckoutAsync(string customerId, OwnerId ownerId, MembershipPlan plan, bool withTrial, Uri returnUrl, CancellationToken cancellationToken = default)
    {
        Checkouts.Add((customerId, plan.Code, withTrial, returnUrl));
        return Task.FromResult(new Uri($"https://checkout.stripe.test/{customerId}"));
    }

    public List<(string CustomerId, string SubscriptionId, string PlanCode)> PlanChanges { get; } = [];

    /// <summary>"Quando la carta rifiuta la differenza": the decline code Stripe answers to the next plan change.</summary>
    public string? DeclinePlanChange { get; set; }

    /// <summary>
    /// UC17 flow 1b as Stripe does it: paid at once, the subscription is on the new plan; refused, it stays as it was and the
    /// failure is the last one of the customer.
    /// </summary>
    public Task<PlanChangeOutcome> ChangePlanAsync(string customerId, string subscriptionId, MembershipPlan plan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        PlanChanges.Add((customerId, subscriptionId, plan.Code));
        var current = Snapshots.GetValueOrDefault(customerId)
            ?? new MembershipSnapshot(MembershipStatus.Active, subscriptionId, null, false, null, null, null, new SubscribedPlan("MySync test", 4.99m, "EUR", "Month", 2));
        if (DeclinePlanChange is { } code)
        {
            var failure = new PaymentFailure(code, "Your card was declined.", DateTimeOffset.UtcNow);
            Snapshots[customerId] = current with { LastFailure = failure };
            return Task.FromResult(new PlanChangeOutcome(false, failure));
        }

        Snapshots[customerId] = current with { Plan = new SubscribedPlan(plan.Name, plan.Price, plan.Currency, plan.Interval, plan.Tier) };
        return Task.FromResult(new PlanChangeOutcome(true, null));
    }

    public Task<Uri> CreatePortalAsync(string customerId, Uri returnUrl, CancellationToken cancellationToken = default)
    {
        Portals.Add((customerId, returnUrl));
        return Task.FromResult(new Uri($"https://billing.stripe.test/{customerId}"));
    }

    public Task<MembershipSnapshot> GetSnapshotAsync(string customerId, CancellationToken cancellationToken = default)
    {
        SnapshotCalls++;
        return Task.FromResult(Snapshots.GetValueOrDefault(customerId, MembershipSnapshot.None));
    }
}
