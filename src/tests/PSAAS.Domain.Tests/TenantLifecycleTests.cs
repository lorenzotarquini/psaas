using Xunit;

namespace PSAAS.Domain.Tests;

/// <summary>Derived from UC13 (stati del cliente).</summary>
public sealed class TenantLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(TenantStatus.Onboarding, TenantStatus.Active, true)]
    [InlineData(TenantStatus.Onboarding, TenantStatus.Trial, true)]
    [InlineData(TenantStatus.Onboarding, TenantStatus.Suspended, false)]
    [InlineData(TenantStatus.Active, TenantStatus.Suspended, true)]
    [InlineData(TenantStatus.Active, TenantStatus.Ceased, false)]
    [InlineData(TenantStatus.Suspended, TenantStatus.Active, true)]
    [InlineData(TenantStatus.Cancelling, TenantStatus.Active, true)]
    [InlineData(TenantStatus.Ceased, TenantStatus.Active, false)]
    public void UC13_S3_OnlyTheAllowedTransitions(TenantStatus from, TenantStatus to, bool allowed)
    {
        Assert.Equal(allowed, TenantLifecycle.New(from, Now).CanChangeTo(to));
    }

    [Fact]
    public void UC13_S1_ChangeKeepsTheReasonAndTheTime_NotTheCreationDate()
    {
        var created = TenantLifecycle.New(TenantStatus.Active, Now);

        var suspended = created.ChangeTo(TenantStatus.Suspended, " Morosità ", Now.AddDays(3));

        Assert.Equal((TenantStatus.Suspended, "Morosità", Now.AddDays(3), Now), (suspended.Status, suspended.Reason, suspended.ChangedAt, suspended.CreatedAt));
    }

    [Fact]
    public void UC13_S4_SuspendingWithoutAReason_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => TenantLifecycle.New(TenantStatus.Active, Now).ChangeTo(TenantStatus.Suspended, " ", Now));
    }

    [Fact]
    public void UC13_S3_ANotAllowedTransition_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => TenantLifecycle.New(TenantStatus.Ceased, Now).ChangeTo(TenantStatus.Active, null, Now));
    }

    [Fact]
    public void Code_IsDerivedFromTheIdentity()
    {
        var tenant = Tenant.Rehydrate(Guid.Parse("8f2c41a9-0000-0000-0000-000000000000"), "Alfa Srl", "12345678901", "IT12345678901", "a@pec.it", new("Via Roma 1", "Roma", "00118", "IT"));

        Assert.Equal("tn_8f2c41a9", tenant.Code);
    }
}
