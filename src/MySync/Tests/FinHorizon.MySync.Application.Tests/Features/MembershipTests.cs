using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Application.Abstractions.Payments;
using FinHorizon.MySync.Application.Features.Memberships;
using FinHorizon.MySync.Domain.Memberships;
using Xunit;

namespace FinHorizon.MySync.Application.Tests.Features;

/// <summary>UC17 at the application level.</summary>
public sealed class MembershipTests : IDisposable
{
    private readonly ApplicationHarness _app = new();

    [Theory]
    [InlineData(MembershipStatus.Active, 2, 3, true)]
    [InlineData(MembershipStatus.Active, 2, 2, false)]
    [InlineData(MembershipStatus.Active, 3, 2, false)]
    [InlineData(MembershipStatus.Active, null, 1, true)]
    [InlineData(MembershipStatus.PastDue, 1, 3, false)]
    [InlineData(MembershipStatus.Expired, 1, 3, false)]
    public void UC17_R10_UpgradeIsAHigherTierOfARunningSubscription(MembershipStatus status, int? currentTier, int targetTier, bool upgrade)
    {
        var membership = new Membership(new Domain.Common.OwnerId("kc-mario"), "cus_1", DateTimeOffset.UtcNow);
        membership.Apply(
            new MembershipSnapshot(status, "sub_1", null, false, null, null, null, new SubscribedPlan("Attuale", 4.99m, "EUR", "Month", currentTier)),
            DateTimeOffset.UtcNow);
        var target = new MembershipPlan("nuovo", "Nuovo", null, targetTier, 9.99m, "EUR", "Month", true, 22m, 0, "price_2", "txr_22");

        Assert.Equal(upgrade, PlanUpgrade.IsUpgrade(membership, target));
    }

    [Fact]
    public async Task UC17_R2_CustomerCreatedOnceAndKept()
    {
        await _app.SendAsync<StartCheckoutCommand, RedirectView>(new StartCheckoutCommand("mysync-mensile", "mario@example.com", "Mario"));
        await _app.SendAsync<StartCheckoutCommand, RedirectView>(new StartCheckoutCommand("mysync-mensile", "mario@example.com", "Mario"));

        Assert.Equal(1, _app.Payments.CustomersCreated);
        Assert.Equal("cus_kc-mario", Assert.Single(_app.Memberships.Items).CustomerId);
        // UC17-R8: the trial only the first time.
        Assert.Equal([("mysync-mensile", true), ("mysync-mensile", false)], _app.Payments.Checkouts);
    }

    [Theory]
    [InlineData(null, "Required")]
    [InlineData("archiviato", "InvalidValue")]
    public async Task UC17_R1_OnlyAPlanOnSale(string? planCode, string code)
    {
        var result = await _app.SendAsync<StartCheckoutCommand, RedirectView>(new StartCheckoutCommand(planCode, null, null));

        Assert.Equal((MembershipErrors.PlanField, code), (result.Errors.Single().Field, result.Errors.Single().Code));
        Assert.Empty(_app.Memberships.Items);
    }

    [Fact]
    public async Task UC17_2b_NoSecondPaymentWhileOpen()
    {
        _app.Memberships.Add(Open(MembershipStatus.PastDue));

        var result = await _app.SendAsync<StartCheckoutCommand, RedirectView>(new StartCheckoutCommand("mysync-mensile", null, null));

        Assert.Equal((MembershipErrors.Field, MembershipErrors.AlreadyActive), (result.Errors.Single().Field, result.Errors.Single().Code));
    }

    [Fact]
    public async Task UC17_1a_PortalNeedsACustomer()
    {
        var result = await _app.SendAsync<OpenBillingPortalCommand, RedirectView>(new OpenBillingPortalCommand());

        Assert.Equal(MembershipErrors.NoCustomer, result.Errors.Single().Code);
    }

    [Fact]
    public async Task UC17_S9_EventsOfOtherCustomersAreIgnored()
    {
        var sync = _app.Get<MembershipSync>();

        var handled = await sync.OnNotificationAsync(new PaymentNotification("evt_1", "invoice.paid", "cus_organization"));
        var withoutCustomer = await sync.OnNotificationAsync(new PaymentNotification("evt_2", "product.created", null));

        Assert.Equal((false, false, 0), (handled, withoutCustomer, _app.Payments.SnapshotCalls));
    }

    [Fact]
    public async Task UC17_R5_EventReadsTheStateAgain()
    {
        _app.Memberships.Add(Open(MembershipStatus.Active));
        _app.Payments.Snapshots["cus_kc-mario"] = MembershipSnapshot.None with { Status = MembershipStatus.PastDue };

        var handled = await _app.Get<MembershipSync>().OnNotificationAsync(new PaymentNotification("evt_1", "invoice.payment_failed", "cus_kc-mario"));

        Assert.True(handled);
        Assert.Equal(MembershipStatus.PastDue, _app.Memberships.Items.Single().Status);
    }

    [Theory]
    [InlineData(1, 2, 1, 5)]
    [InlineData(2, 10, 10, 20)]
    [InlineData(3, 100, 50, 100)]
    public async Task C2_C6_LimitsOfTheTierPaid(int tier, int accounts, int debts, int subscriptions)
    {
        var membership = Open(MembershipStatus.Active, tier);
        _app.Memberships.Add(membership);

        var limits = await Limits().ForAsync(FakeStore.Mario);

        Assert.Equal(new UserLimits(accounts, debts, subscriptions), limits);
    }

    [Fact]
    public async Task C4_WithoutATier_TheLowestLimitsAreShownAndNothingCanBeWritten()
    {
        var limits = await Limits().ForAsync(FakeStore.Mario);

        Assert.Equal(new UserLimits(2, 1, 5), limits);
        Assert.False(await _app.Get<MembershipSync>().CanWriteAsync(FakeStore.Mario));
    }

    [Fact]
    public async Task C3_PaidButForALevelMySyncDoesNotDeclare_IsNoTier()
    {
        _app.Memberships.Add(Open(MembershipStatus.Active, 4));

        Assert.False((await _app.Get<MembershipSync>().EntitlementAsync(FakeStore.Mario)).CanWrite);
    }

    private TierResourceLimits Limits() =>
        new(_app.Get<MembershipSync>(), new DeclaredModuleTiers(), Microsoft.Extensions.Options.Options.Create(new LimitsOptions()));

    private static Membership Open(MembershipStatus status, int tier = 2)
    {
        var membership = new Membership(FakeStore.Mario, "cus_kc-mario", DateTimeOffset.UnixEpoch);
        membership.Apply(MembershipSnapshot.None with { Status = status, Plan = new SubscribedPlan("MySync", 4.99m, "EUR", "Month", tier) }, DateTimeOffset.UnixEpoch);
        return membership;
    }

    public void Dispose() => _app.Dispose();
}
