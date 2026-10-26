using Microsoft.Extensions.Time.Testing;
using PSAAS.Application.Abstractions.Communications;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Billing;
using PSAAS.Application.Contracts.Catalog;
using PSAAS.Application.Contracts.Communications;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Services.Billing;
using PSAAS.Application.Services.Communications;
using PSAAS.Application.Services.Tenants;
using PSAAS.Domain;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC27 (comunicazioni) and UC28 (cose da gestire).</summary>
public sealed class CommunicationServiceTests
{
    /// <summary>The tenant list as the segment query reads it: by status and by explicit ids.</summary>
    private sealed class FakeList : ITenantListRepository
    {
        public List<TenantListItem> Items { get; } = [];

        public TenantListQuery? LastQuery { get; private set; }

        public Task<PagedResult<TenantListItem>> GetListAsync(TenantListQuery query, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            var items = Items
                .Where(t => query.Statuses is not { Count: > 0 } || query.Statuses.Contains(t.Status))
                .Where(t => query.Ids is not { Count: > 0 } || query.Ids.Contains(t.Id))
                .ToArray();
            return Task.FromResult(new PagedResult<TenantListItem>(items, items.Length, 1, query.PageSize));
        }

        public Task<TenantCounts> GetCountsAsync(DateTimeOffset now, int renewalDays, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeCommunications : ICommunicationRepository
    {
        public List<Communication> Saved { get; } = [];

        public Task AddAsync(Communication communication, CancellationToken cancellationToken = default)
        {
            Saved.Add(communication);
            return Task.CompletedTask;
        }

        public Task<PagedResult<Communication>> GetPageAsync(Guid? tenantId, int page, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Communication?> GetAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private readonly FakeList _list = new();
    private readonly FakeCommunications _communications = new();
    private readonly FakeEmailSender _email = new();
    private readonly FakeAuditLog _audit = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.Zero));

    private CommunicationService Service => new(_list, _communications, _email, _audit, new FakeCurrentActor(), _clock);

    private static TenantListItem Tenant(string name, string status = "Active") => new(
        Guid.NewGuid(), name, "1", "IT1", $"{name.Split(' ')[0].ToUpperInvariant()}@PEC.IT", "Roma", "RM", "tn_1", status, DateTimeOffset.UtcNow,
        null, null, null, null, null, [], []);

    private static CommunicationRequest Request(string[] channels, CommunicationSegment? segment = null, DateOnly? visibleUntil = null) =>
        new("Manutenzione", "Sabato fermi", channels, segment ?? new CommunicationSegment(), visibleUntil);

    [Fact]
    public async Task UC27_R1_CeasedTenantsAreNeverReached()
    {
        _list.Items.AddRange([Tenant("Alfa Srl"), Tenant("Delta Srl", "Ceased")]);

        var (preview, _) = await Service.PreviewAsync(new CommunicationSegment(Statuses: ["Ceased", "Active"]));

        Assert.Equal(["Alfa Srl"], preview!.Sample);
        Assert.DoesNotContain("Ceased", _list.LastQuery!.Statuses!);
    }

    [Fact]
    public async Task UC27_R4_EmailsGoToTheLowercasePec_OneAuditEventPerTenant()
    {
        _list.Items.AddRange([Tenant("Alfa Srl"), Tenant("Beta Snc")]);

        var result = await Service.SendAsync(Request([CommunicationChannels.Email]));

        Assert.Equal(["alfa@pec.it", "beta@pec.it"], _email.Sent.Select(m => m.To).Order());
        Assert.Equal(2, _audit.Records.Count(r => r.Action == "communication.sent"));
        Assert.Null(result.Communication!.VisibleUntil);
    }

    [Fact]
    public async Task UC27_4a_EmailNotConfigured_IsSkippedButTheAnnouncementIsKept()
    {
        _list.Items.Add(Tenant("Alfa Srl"));
        _email.IsConfigured = false;

        var result = await Service.SendAsync(Request([CommunicationChannels.InApp, CommunicationChannels.Email]));

        Assert.Equal(EmailStatuses.Skipped, Assert.Single(result.Communication!.Recipients).EmailStatus);
        Assert.Equal(new DateOnly(2026, 11, 19), result.Communication.VisibleUntil);
        Assert.Single(_communications.Saved);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(91)]
    public async Task UC27_R3_VisibleUntilWithin90Days(int days)
    {
        _list.Items.Add(Tenant("Alfa Srl"));

        var result = await Service.SendAsync(Request([CommunicationChannels.InApp], visibleUntil: new DateOnly(2026, 10, 20).AddDays(days)));

        Assert.Equal("InvalidValue", result.Errors["visibleUntil"]);
        Assert.Empty(_communications.Saved);
    }

    [Fact]
    public async Task UC27_1a_UnknownChannelAndStatus_AreInvalid()
    {
        var result = await Service.SendAsync(Request(["Fax"], new CommunicationSegment(Statuses: ["Happy"])));

        Assert.Equal(("InvalidValue", "InvalidValue"), (result.Errors["channels"], result.Errors["segment.statuses"]));
    }

    [Fact]
    public async Task UC28_R3_CriticalFirstThenOldest()
    {
        var subscriptions = new FakeSubscriptionRepository();
        var invoices = new FakeInvoiceRepository();
        var tenantId = Guid.NewGuid();
        subscriptions.Subscriptions[tenantId] = new(tenantId, "Beta Snc", Guid.NewGuid(), "Pro", SubscriptionStatuses.Trial, 0, "Monthly", null, null,
            _clock.GetUtcNow(), _clock.GetUtcNow().AddDays(2), _clock.GetUtcNow().AddDays(2), true, [], []);
        await invoices.AddAsync(new Invoice(Guid.NewGuid(), "", InvoiceKinds.Invoice, Guid.NewGuid(), "Alfa Srl", "IT1", "a@pec.it", null,
            _clock.GetUtcNow().AddDays(-40), _clock.GetUtcNow().AddDays(-10), null, null, [], 1, 0.22m, 0.22m, 1.22m, InvoiceStatuses.Overdue));
        var service = new NotificationService(subscriptions, invoices, _list, new FakeTenantReadRepository(), new FakeUserDirectory(), _clock);

        var queue = await service.GetAsync();

        Assert.Equal([NotificationKinds.InvoiceOverdue, NotificationKinds.TrialEnding], queue.Select(n => n.Kind));
        Assert.Equal(2, queue[1].Value);
    }

    [Fact]
    public async Task UC24_R3_ReminderEmailedToThePec_RefusalDoesNotStopTheRun()
    {
        var tenants = new FakeTenantReadRepository
        {
            Tenant = Domain.Tenant.Create("Alfa Srl", "11111111111", "IT11111111111", "ALFA@PEC.IT", new Address("Via Roma 1", "Roma", "00118", "IT")),
        };
        var invoices = new FakeInvoiceRepository();
        var subscriptions = new FakeSubscriptionRepository();
        var lifecycle = new TenantLifecycleService(tenants, new FakeTenantWriteRepository(), _audit, _clock, subscriptions);
        BillingService Billing() => new(invoices, new FakeInvoiceTransmitter(), subscriptions, tenants, lifecycle, _email, _audit, new BillingOptions(0), _clock);
        var subscription = new SubscriptionReadModel(tenants.Tenant.Id, "Alfa Srl", Guid.NewGuid(), "Pro", SubscriptionStatuses.Active, 100, "Monthly",
            null, null, _clock.GetUtcNow(), null, _clock.GetUtcNow().AddDays(30), false, [], []);
        await Billing().InvoicePeriodAsync(subscription, _clock.GetUtcNow());
        _clock.Advance(TimeSpan.FromDays(31));

        await Billing().RunAsync();
        _email.Rejecting.Add("alfa@pec.it");
        _clock.Advance(TimeSpan.FromDays(7));
        var second = await Billing().RunAsync();

        var reminder = Assert.Single(_email.Sent);
        Assert.Equal(("alfa@pec.it", "Sollecito di pagamento fattura 2026/0001"), (reminder.To, reminder.Subject));
        Assert.Contains("122,00 €", reminder.Body);
        Assert.Equal(1, second.Reminders);
    }
}
