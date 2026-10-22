using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Services.Tenants;
using PSAAS.Domain;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC13 (stato del cliente) and UC14 (annullare una bozza).</summary>
public sealed class TenantLifecycleServiceTests
{
    private static Tenant TenantIn(TenantStatus status) => Tenant.Create(
        "Alfa Srl", "11111111111", "IT11111111111", "alfa@pec.it", new Address("Via Roma 1", "Roma", "00118", "IT"), status);

    private readonly FakeTenantReadRepository _reader = new();
    private readonly FakeTenantWriteRepository _writer = new();
    private readonly FakeAuditLog _audit = new();

    private TenantLifecycleService Service => new(_reader, _writer, _audit, TimeProvider.System, new FakeSubscriptionRepository());

    [Fact]
    public async Task UC13_S1_SuspendsWithTheReasonAndAudits()
    {
        _reader.Tenant = TenantIn(TenantStatus.Active);

        var result = await Service.ChangeStatusAsync(_reader.Tenant.Id, new("suspended", " Morosità "));

        Assert.True(result.Succeeded);
        Assert.Equal((TenantStatus.Suspended, "Morosità"), (_writer.SavedLifecycle!.Status, _writer.SavedLifecycle.Reason));
        Assert.Equal("tenant.status.changed", Assert.Single(_audit.Records).Action);
    }

    [Theory]
    [InlineData(TenantStatus.Ceased, "Active", "status")]
    [InlineData(TenantStatus.Active, "Paused", "status")]
    [InlineData(TenantStatus.Active, "Cancelling", "reason")]
    public async Task UC13_S3_S4_RejectsTransitionsAndMissingReasons(TenantStatus from, string to, string field)
    {
        _reader.Tenant = TenantIn(from);

        var result = await Service.ChangeStatusAsync(_reader.Tenant.Id, new(to, null));

        Assert.True(result.Errors.ContainsKey(field));
        Assert.Null(_writer.SavedLifecycle);
        Assert.Empty(_audit.Records);
    }

    [Fact]
    public async Task UC14_S3_S4_OnlyADraftCanBeDiscarded()
    {
        _reader.Tenant = TenantIn(TenantStatus.Active);
        Assert.Equal(DiscardResult.NotADraft, await Service.DiscardDraftAsync(_reader.Tenant.Id));
        Assert.False(_writer.Deleted);

        _reader.Tenant = TenantIn(TenantStatus.Onboarding);
        Assert.Equal(DiscardResult.Discarded, await Service.DiscardDraftAsync(_reader.Tenant.Id));
        Assert.True(_writer.Deleted);
        Assert.Equal("tenant.onboarding.discarded", Assert.Single(_audit.Records).Action);
    }
}
