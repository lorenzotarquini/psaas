using Microsoft.Extensions.Time.Testing;
using PSAAS.Application.Contracts.Billing;
using PSAAS.Application.Contracts.Catalog;
using PSAAS.Application.Services.Billing;
using PSAAS.Application.Services.Tenants;
using PSAAS.Domain;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC23 (fatturazione) and UC24 (incassi e solleciti).</summary>
public sealed class BillingServiceTests
{
    private readonly FakeTenantReadRepository _tenants = new();
    private readonly FakeTenantWriteRepository _writer = new();
    private readonly FakeSubscriptionRepository _subscriptions = new();
    private readonly FakeInvoiceRepository _invoices = new();
    private readonly FakeAuditLog _audit = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));

    public BillingServiceTests() =>
        _tenants.Tenant = Tenant.Create("Alfa Srl", "11111111111", "IT11111111111", "alfa@pec.it", new Address("Via Roma 1", "Roma", "00118", "IT"));

    private Guid TenantId => _tenants.Tenant!.Id;

    private BillingService Service(int suspendAfterDays = 30) => new(
        _invoices, new FakeInvoiceTransmitter(), _subscriptions, _tenants,
        new TenantLifecycleService(_tenants, _writer, _audit, _clock, _subscriptions),
        new FakeEmailSender(), _audit, new BillingOptions(suspendAfterDays), _clock);

    private SubscriptionReadModel Subscription(decimal price, string period = BillingPeriods.Monthly, bool autoRenew = true, DateTimeOffset? renewsAt = null) =>
        new(TenantId, "Alfa Srl", Guid.NewGuid(), "Pro", SubscriptionStatuses.Active, price, period, null, null,
            _clock.GetUtcNow(), null, renewsAt ?? _clock.GetUtcNow().AddDays(30), autoRenew, [], []);

    [Fact]
    public async Task UC23_R2_VatAndDueDate()
    {
        await Service().InvoicePeriodAsync(Subscription(99.99m), _clock.GetUtcNow());

        var invoice = Assert.Single(_invoices.Invoices);
        Assert.Equal((99.99m, 22.00m, 121.99m), (invoice.Net, invoice.Vat, invoice.Total));
        Assert.Equal(_clock.GetUtcNow().AddDays(30), invoice.DueAt);
        Assert.Equal("Piano Pro · 01/10/2026 – 30/10/2026", invoice.Lines[0].Description);
    }

    [Fact]
    public async Task UC23_FreePlan_HasNoInvoice()
    {
        await Service().InvoicePeriodAsync(Subscription(0), _clock.GetUtcNow());

        Assert.Empty(_invoices.Invoices);
    }

    [Fact]
    public async Task UC23_R4_ProRataRoundsToTheCent()
    {
        var before = Subscription(100);
        var after = before with { Price = 110, PlanName = "Pro+" };
        _clock.Advance(TimeSpan.FromDays(19));

        await Service().InvoicePlanChangeAsync(before, after);

        // 10 € / 30 days × 11 days left = 3.666… → 3.67
        Assert.Equal(3.67m, Assert.Single(_invoices.Invoices).Net);
    }

    [Fact]
    public async Task UC23_R6_MissedRenewalsAreCaughtUpOnce()
    {
        var subscription = Subscription(100, renewsAt: _clock.GetUtcNow());
        _subscriptions.Subscriptions[TenantId] = subscription;
        _clock.Advance(TimeSpan.FromDays(65));

        var first = await Service().RunAsync();
        var second = await Service().RunAsync();

        Assert.Equal((3, 0), (first.Issued, second.Issued));
        Assert.Equal(subscription.RenewsAt.AddDays(90), _subscriptions.Subscriptions[TenantId].RenewsAt);
    }

    [Fact]
    public async Task UC24_PaidInvoice_CannotBeCancelledOrPaidAgain()
    {
        await Service().InvoicePeriodAsync(Subscription(100), _clock.GetUtcNow());
        var id = _invoices.Invoices[0].Id;

        Assert.Equal(InvoiceStatuses.Paid, (await Service().RegisterPaymentAsync(id, new("2026-10-01", " CRO 1 "))).Invoice!.Status);
        Assert.True((await Service().CancelAsync(id, new("errore"))).Conflict);
        Assert.True((await Service().RegisterPaymentAsync(id, new("2026-10-01", null))).Conflict);
        Assert.False((await Service().CancelAsync(Guid.NewGuid(), new("x"))).Found);
    }

    [Fact]
    public async Task UC24_Cancel_RequiresAReason()
    {
        await Service().InvoicePeriodAsync(Subscription(100), _clock.GetUtcNow());

        Assert.Equal("Required", (await Service().CancelAsync(_invoices.Invoices[0].Id, new(" "))).Errors["reason"]);
    }

    [Theory]
    [InlineData(TenantStatus.Active, 1)]
    [InlineData(TenantStatus.Cancelling, 0)]
    public async Task UC24_R4_SuspendsOnlyRunningTenants(TenantStatus status, int expected)
    {
        _tenants.Tenant = Tenant.Rehydrate(
            TenantId, "Alfa Srl", "11111111111", "IT11111111111", "alfa@pec.it", new Address("Via Roma 1", "Roma", "00118", "IT"),
            new TenantLifecycle(status, "x", _clock.GetUtcNow(), _clock.GetUtcNow()), new TenantCommercialInfo(null, null));
        await Service().InvoicePeriodAsync(Subscription(100), _clock.GetUtcNow());
        _clock.Advance(TimeSpan.FromDays(61));

        Assert.Equal(expected, (await Service().RunAsync()).Suspended);
    }

    [Fact]
    public async Task UC24_R4_ZeroDaysNeverSuspends()
    {
        await Service(0).InvoicePeriodAsync(Subscription(100), _clock.GetUtcNow());
        _clock.Advance(TimeSpan.FromDays(200));

        Assert.Equal(0, (await Service(0).RunAsync()).Suspended);
    }
}
