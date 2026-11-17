using FinHorizon.AdminPortal.Application.Contracts.Payments;

namespace FinHorizon.AdminPortal.Application.Abstractions.Payments;

/// <summary>The events received from the payment provider, one per id (UC33-R2).</summary>
public interface IPaymentEventStore
{
    /// <summary>Stores the event; false when an event with the same id is already there, also under concurrency.</summary>
    Task<bool> TryAddAsync(ReceivedPaymentEvent paymentEvent, CancellationToken cancellationToken = default);

    /// <summary>The latest received events, newest first.</summary>
    Task<IReadOnlyList<ReceivedPaymentEvent>> GetLatestAsync(int take, CancellationToken cancellationToken = default);
}
