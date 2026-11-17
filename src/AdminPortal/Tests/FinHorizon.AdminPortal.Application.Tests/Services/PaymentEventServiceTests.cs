using Microsoft.Extensions.Time.Testing;
using FinHorizon.AdminPortal.Application.Abstractions.Payments;
using FinHorizon.AdminPortal.Application.Contracts.Payments;
using FinHorizon.AdminPortal.Application.Services.Payments;
using Xunit;

namespace FinHorizon.AdminPortal.Application.Tests.Services;

/// <summary>Derived from UC33 (eventi di Stripe).</summary>
public sealed class PaymentEventServiceTests
{
    private readonly FakePaymentEventStore _store = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.Zero));

    private PaymentEventService Service() => new(_store, _clock);

    [Fact]
    public async Task UC33_S1_RegistratoConLaDataDiRicezione()
    {
        var created = new DateTimeOffset(2026, 11, 17, 8, 59, 0, TimeSpan.Zero);

        var outcome = await Service().ReceiveAsync(new PaymentEvent("evt_1", "invoice.paid", created));

        Assert.Equal(PaymentEventOutcome.Recorded, outcome);
        Assert.Equal(new ReceivedPaymentEvent("evt_1", "invoice.paid", created, _clock.GetUtcNow()), Assert.Single(_store.Events));
    }

    [Fact]
    public async Task UC33_R2_StessoIdUnaVolta()
    {
        var paymentEvent = new PaymentEvent("evt_1", "invoice.paid", _clock.GetUtcNow());
        await Service().ReceiveAsync(paymentEvent);

        var outcome = await Service().ReceiveAsync(paymentEvent);

        Assert.Equal(PaymentEventOutcome.Duplicate, outcome);
        Assert.Single(_store.Events);
    }

    private sealed class FakePaymentEventStore : IPaymentEventStore
    {
        public List<ReceivedPaymentEvent> Events { get; } = [];

        public Task<bool> TryAddAsync(ReceivedPaymentEvent paymentEvent, CancellationToken cancellationToken = default)
        {
            if (Events.Any(e => e.Id == paymentEvent.Id))
            {
                return Task.FromResult(false);
            }

            Events.Add(paymentEvent);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<ReceivedPaymentEvent>> GetLatestAsync(int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ReceivedPaymentEvent>>(Events.OrderByDescending(e => e.ReceivedAt).Take(take).ToList());
    }
}
