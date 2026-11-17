using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Memberships;
using Xunit;

namespace FinHorizon.MySync.Domain.Tests.Memberships;

/// <summary>UC17-R3, R4, R5.</summary>
public sealed class MembershipTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UC17_R3_NewCustomerIsExpiredUntilStripeSaysOtherwise()
    {
        var membership = new Membership(new OwnerId("kc-luca"), "cus_1", Now);

        Assert.Equal((MembershipStatus.Expired, false, false), (membership.Status, membership.CanWrite, membership.IsOpen));
    }

    [Theory]
    [InlineData(MembershipStatus.Active, true)]
    [InlineData(MembershipStatus.PastDue, true)]
    [InlineData(MembershipStatus.Expired, false)]
    public void UC17_R4_WritesWhileActiveOrRetrying(MembershipStatus status, bool canWrite)
    {
        var membership = new Membership(new OwnerId("kc-luca"), "cus_1", Now);

        membership.Apply(MembershipSnapshot.None with { Status = status }, Now);

        Assert.Equal(canWrite, membership.CanWrite);
    }

    [Fact]
    public void UC17_R5_SnapshotReplacesEverything()
    {
        var membership = new Membership(new OwnerId("kc-luca"), "cus_1", Now);
        membership.Apply(new MembershipSnapshot(MembershipStatus.PastDue, "sub_1", Now, false, new PaymentCard("visa", "4242", 1, 2030), new PaymentFailure("card_declined", null, Now), new Uri("https://pay.test")), Now);

        membership.Apply(MembershipSnapshot.None with { Status = MembershipStatus.Active }, Now.AddDays(1));

        Assert.Equal((MembershipStatus.Active, null, null, null, Now.AddDays(1)), (membership.Status, membership.LastFailure, membership.PayUrl, membership.Card, membership.UpdatedAt));
    }
}
