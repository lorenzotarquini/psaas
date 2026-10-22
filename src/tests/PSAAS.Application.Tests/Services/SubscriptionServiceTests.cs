using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Catalog;
using PSAAS.Application.Services.Catalog;
using PSAAS.Application.Services.Tenants;
using PSAAS.Domain;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC17 (sottoscrizione) and UC18 (provisioning).</summary>
public sealed class SubscriptionServiceTests
{
    private readonly FakeTenantReadRepository _tenants = new();
    private readonly FakeCatalogRepository _catalog = new();
    private readonly FakeSubscriptionRepository _subscriptions = new();
    private readonly FakeModuleProvisioner _provisioner = new();
    private readonly FakeUserDirectory _users = new();
    private readonly FakeAuditLog _audit = new();

    private Guid TenantId => _tenants.Tenant!.Id;

    public SubscriptionServiceTests() =>
        _tenants.Tenant = Tenant.Create("Alfa Srl", "11111111111", "IT11111111111", "alfa@pec.it", new Address("Via Roma 1", "Roma", "00118", "IT"));

    private SubscriptionService Service => new(
        _tenants, _catalog, _subscriptions, _provisioner, _users,
        new TenantLifecycleService(_tenants, new FakeTenantWriteRepository(), _audit, TimeProvider.System, _subscriptions),
        _audit, TimeProvider.System);

    private PlanReadModel GivenPlan(int? maxUsers = null, int trialDays = 0, params string[] modules)
    {
        var plan = FakeCatalogRepository.Plan("pro", maxUsers, trialDays, modules);
        _catalog.Plans.Add(plan);
        return plan;
    }

    [Fact]
    public void WithDependencies_AddsRequiredModulesTransitively()
    {
        var modules = new[] { FakeCatalogRepository.Module("a", requires: ["b"]), FakeCatalogRepository.Module("b", requires: ["c"]), FakeCatalogRepository.Module("c") }
            .ToDictionary(m => m.Id);

        Assert.Equal(["a", "b", "c"], SubscriptionService.WithDependencies(["a"], modules).Order());
    }

    [Fact]
    public async Task UC17_S1_SavesThePlanModulesWithCoreAndProvisionsThem()
    {
        _catalog.Modules.Add(FakeCatalogRepository.Module("warehouse"));
        var plan = GivenPlan(modules: "warehouse");

        var result = await Service.SubscribeAsync(TenantId, new(plan.Id));

        Assert.True(result.Succeeded);
        Assert.Equal(["core", "warehouse"], _provisioner.Provisioned.Order());
        Assert.All(_subscriptions.Modules.Values, m => Assert.Equal(ProvisioningStatuses.Active, m.Status));
        Assert.Contains(_audit.Records, r => r.Action == "subscription.changed");
    }

    [Fact]
    public async Task UC17_R2_TrialOnlyOnTheFirstSubscription()
    {
        var plan = GivenPlan(trialDays: 14);

        await Service.SubscribeAsync(TenantId, new(plan.Id, Trial: true));
        var first = _subscriptions.Subscriptions[TenantId];
        await Service.SubscribeAsync(TenantId, new(plan.Id, Trial: true));

        Assert.Equal(SubscriptionStatuses.Trial, first.Status);
        Assert.Equal(SubscriptionStatuses.Active, _subscriptions.Subscriptions[TenantId].Status);
    }

    [Fact]
    public async Task UC17_S3_MoreUsersThanThePlanAllows_IsLimitExceeded()
    {
        _tenants.Structure = new([new("p1", "Operatore", null)], []);
        _users.Users.Add(new("u1", "Mario", "Rossi", "m@x.it", null, ["p1"]));
        _users.Users.Add(new("u2", "Anna", "Bianchi", "a@x.it", null, ["p1"]));
        var plan = GivenPlan(maxUsers: 1);

        var result = await Service.SubscribeAsync(TenantId, new(plan.Id));

        Assert.Equal("LimitExceeded", result.Errors["planId"]);
        Assert.Empty(_subscriptions.Subscriptions);
    }

    [Fact]
    public async Task UC17_2b_UnknownPlanAndAddOn_AreInvalidValue()
    {
        var result = await Service.SubscribeAsync(TenantId, new(Guid.NewGuid(), AddOnModuleIds: ["nope"]));

        Assert.Equal(("InvalidValue", "InvalidValue"), (result.Errors["planId"], result.Errors["addOnModuleIds[0]"]));
    }

    [Fact]
    public async Task UC18_FailedHook_MarksTheModuleFailedAndRetryActivatesIt()
    {
        _catalog.Modules.Add(FakeCatalogRepository.Module("warehouse"));
        _provisioner.Failing.Add("warehouse");
        await Service.SubscribeAsync(TenantId, new(GivenPlan(modules: "warehouse").Id));
        var failed = _subscriptions.Modules[(TenantId, "warehouse")];

        _provisioner.Failing.Clear();
        var retried = await Service.RetryFailedAsync();

        Assert.Equal((ProvisioningStatuses.Failed, "timeout"), (failed.Status, failed.Error));
        Assert.Equal(1, retried);
        Assert.Equal((ProvisioningStatuses.Active, 2), (_subscriptions.Modules[(TenantId, "warehouse")].Status, _subscriptions.Modules[(TenantId, "warehouse")].Attempts));
    }

    [Fact]
    public async Task UC18_RetryOfAnActiveModule_IsInvalidValue()
    {
        await Service.SubscribeAsync(TenantId, new(GivenPlan().Id));

        var result = await Service.RetryAsync(TenantId, "core");

        Assert.Equal("InvalidValue", result.Errors["moduleId"]);
    }
}
