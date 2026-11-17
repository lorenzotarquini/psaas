using NLog;
using FinHorizon.AdminPortal.Application.Abstractions.Payments;
using FinHorizon.AdminPortal.Application.Contracts.Payments;

namespace FinHorizon.AdminPortal.Application.Services.Payments;

/// <summary>
/// UC33: every verified event of Stripe is stored once. Reactions to single event types (tenant status, modules,
/// invoices) are added here in the next phases of docs/admin-portal/STRIPE.md.
/// </summary>
public sealed class PaymentEventService(IPaymentEventStore store, TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<PaymentEventOutcome> ReceiveAsync(PaymentEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentEvent);
        var received = new ReceivedPaymentEvent(paymentEvent.Id, paymentEvent.Type, paymentEvent.CreatedAt, clock.GetUtcNow());
        if (!await store.TryAddAsync(received, cancellationToken).ConfigureAwait(false))
        {
            // UC33-R2: Stripe delivers at least once; a repeated event changes nothing.
            Logger.Info("Stripe event {EventId} ({EventType}) already received, ignored", paymentEvent.Id, paymentEvent.Type);
            return PaymentEventOutcome.Duplicate;
        }

        // UC33-R4: id and type only, never customer data.
        Logger.Info("Stripe event {EventId} ({EventType}) recorded", paymentEvent.Id, paymentEvent.Type);
        return PaymentEventOutcome.Recorded;
    }

    public Task<IReadOnlyList<ReceivedPaymentEvent>> GetLatestAsync(int take, CancellationToken cancellationToken = default) =>
        store.GetLatestAsync(take, cancellationToken);
}
