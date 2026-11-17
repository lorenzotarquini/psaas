using FinHorizon.MySync.Application.Abstractions.Payments;
using FinHorizon.MySync.Application.Features.Memberships;
using FinHorizon.MySync.Domain.Memberships;
using Xunit;

namespace FinHorizon.MySync.Application.Tests.Features;

/// <summary>UC17 at the application level.</summary>
public sealed class MembershipTests : IDisposable
{
    private readonly ApplicationHarness _app = new();

    [Fact]
    public async Task UC17_R2_CustomerCreatedOnceAndKept()
    {
        await _app.SendAsync<StartCheckoutCommand, RedirectView>(new StartCheckoutCommand("mario@example.com", "Mario"));
        await _app.SendAsync<StartCheckoutCommand, RedirectView>(new StartCheckoutCommand("mario@example.com", "Mario"));

        Assert.Equal(1, _app.Payments.CustomersCreated);
        Assert.Equal("cus_kc-mario", Assert.Single(_app.Memberships.Items).CustomerId);
    }

    [Fact]
    public async Task UC17_2b_NoSecondPaymentWhileOpen()
    {
        _app.Memberships.Add(Open(MembershipStatus.PastDue));

        var result = await _app.SendAsync<StartCheckoutCommand, RedirectView>(new StartCheckoutCommand(null, null));

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

    private static Membership Open(MembershipStatus status)
    {
        var membership = new Membership(FakeStore.Mario, "cus_kc-mario", DateTimeOffset.UnixEpoch);
        membership.Apply(MembershipSnapshot.None with { Status = status }, DateTimeOffset.UnixEpoch);
        return membership;
    }

    public void Dispose() => _app.Dispose();
}
