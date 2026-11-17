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

    public List<(string CustomerId, Uri ReturnUrl)> Checkouts { get; } = [];

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

    public Task<Uri> CreateCheckoutAsync(string customerId, OwnerId ownerId, Uri returnUrl, CancellationToken cancellationToken = default)
    {
        Checkouts.Add((customerId, returnUrl));
        return Task.FromResult(new Uri($"https://checkout.stripe.test/{customerId}"));
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
