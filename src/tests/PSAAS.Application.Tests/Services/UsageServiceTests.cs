using Microsoft.Extensions.Time.Testing;
using PSAAS.Application.Abstractions.Health;
using PSAAS.Application.Abstractions.Usage;
using PSAAS.Application.Contracts.Catalog;
using PSAAS.Application.Contracts.Usage;
using PSAAS.Application.Services.Health;
using PSAAS.Application.Services.Usage;
using PSAAS.Domain;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC25 (uso del cliente) and UC26 (stato dei servizi).</summary>
public sealed class UsageServiceTests
{
    private sealed class FakeUsage : IUsageRepository
    {
        public List<UsageRecord> Saved { get; } = [];

        public Task AddAsync(IReadOnlyList<UsageRecord> records, CancellationToken cancellationToken = default)
        {
            Saved.AddRange(records);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<UsageRecord>> GetAsync(Guid tenantId, DateOnly from, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UsageRecord>>(Saved.Where(r => r.TenantId == tenantId && r.Day >= from).ToArray());
    }

    private sealed class Probe(string key, Func<Task<ProbeResult>> check) : IServiceProbe
    {
        public string Key => key;

        public Task<ProbeResult> CheckAsync(CancellationToken cancellationToken) => check();
    }

    private readonly FakeUsage _usage = new();
    private readonly FakeTenantReadRepository _tenants = new();
    private readonly FakeSubscriptionRepository _subscriptions = new();
    private readonly FakeUserDirectory _users = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));

    public UsageServiceTests()
    {
        _tenants.Tenant = Tenant.Create("Alfa Srl", "11111111111", "IT11111111111", "alfa@pec.it", new Address("Via Roma 1", "Roma", "00118", "IT"));
        _subscriptions.Subscriptions[TenantId] = new(TenantId, "Alfa Srl", Guid.NewGuid(), "Pro", "Active", 100, "Monthly", 10, 3,
            _clock.GetUtcNow(), null, _clock.GetUtcNow().AddDays(30), true, [], []);
        _subscriptions.Modules[(TenantId, "warehouse")] = (ProvisioningStatuses.Active, null, 1);
    }

    private Guid TenantId => _tenants.Tenant!.Id;

    private UsageService Service => new(_usage, _tenants, _subscriptions, new FakeCatalogRepository(), _users, _clock);

    private UsageRecordRequest Row(string metric = "documents.created", decimal value = 1, string? kind = null, DateTimeOffset? at = null, string module = "warehouse") =>
        new(TenantId, module, metric, value, kind, at);

    [Theory]
    [InlineData("Documents Created", 1, null, 0, "metric", "InvalidFormat")]
    [InlineData("storage.mb", 1, "average", 0, "kind", "InvalidValue")]
    [InlineData("storage.mb", 1, null, 1, "at", "InvalidValue")]
    [InlineData("storage.mb", 1, null, -400, "at", "InvalidValue")]
    public async Task UC25_S3_InvalidRows_AreRejectedAndNothingIsSaved(string metric, decimal value, string? kind, int dayOffset, string field, string code)
    {
        var errors = await Service.ReportAsync(new([Row(), Row(metric, value, kind, _clock.GetUtcNow().AddDays(dayOffset))]));

        Assert.Equal(code, errors[$"records[1].{field}"]);
        Assert.Empty(_usage.Saved);
    }

    [Fact]
    public async Task UC25_R2_EmptyOrTooLargeReports_AreRejected()
    {
        Assert.Equal("InvalidValue", (await Service.ReportAsync(new([])))["records"]);
        Assert.Equal("InvalidValue", (await Service.ReportAsync(new(Enumerable.Repeat(Row(), 501).ToArray())))["records"]);
    }

    [Fact]
    public async Task UC25_R3_CountersSumOverThePeriod_GaugesKeepTheLastDay()
    {
        await Service.ReportAsync(new([
            Row("documents.created", 3, at: _clock.GetUtcNow().AddDays(-2)), Row("documents.created", 4),
            Row("storage.mb", 120, "gauge", _clock.GetUtcNow().AddDays(-2)), Row("storage.mb", 90, "gauge"),
        ]));

        var usage = await Service.GetTenantUsageAsync(TenantId, 30);

        Assert.Equal(7m, usage!.Metrics.Single(m => m.Metric == "documents.created").Total);
        Assert.Equal(90m, usage.Metrics.Single(m => m.Metric == "storage.mb").Total);
    }

    [Fact]
    public async Task UC25_2a_KeycloakDown_LeavesTheUserCountsEmpty()
    {
        _tenants.Structure = new([new("p1", "Operatore", null)], []);
        _users.Unavailable = true;

        var usage = await Service.GetTenantUsageAsync(TenantId, 30);

        Assert.Equal((null, null, 10), (usage!.Users, usage.ActiveUsers, usage.MaxUsers));
    }

    [Fact]
    public async Task UC26_FailingAndSlowProbesAreDown_NotConfiguredDoesNotCount()
    {
        var service = new ServicesHealthService(
        [
            new Probe("database", () => Task.FromResult(new ProbeResult(ServiceStatuses.Up))),
            new Probe("keycloak-admin", () => throw new InvalidOperationException("401")),
            new Probe("invoice-transmission", () => Task.FromResult(new ProbeResult(ServiceStatuses.NotConfigured))),
        ], _clock);

        var health = await service.CheckAsync();

        Assert.Equal("Down", health.Status);
        Assert.Equal(("Down", "401"), (health.Services[1].Status, health.Services[1].Detail));
    }

    [Fact]
    public async Task UC26_R3_WorstConfiguredStatusWins()
    {
        var service = new ServicesHealthService(
        [
            new Probe("database", () => Task.FromResult(new ProbeResult(ServiceStatuses.Up))),
            new Probe("module-hooks", () => Task.FromResult(new ProbeResult(ServiceStatuses.Degraded, "2"))),
            new Probe("invoice-transmission", () => Task.FromResult(new ProbeResult(ServiceStatuses.NotConfigured))),
        ], _clock);

        Assert.Equal("Degraded", (await service.CheckAsync()).Status);
    }
}
