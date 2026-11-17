using FinHorizon.MySync.Application.Abstractions.Payments;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Memberships;
using NLog;

namespace FinHorizon.MySync.Application.Features.Memberships;

/// <summary>
/// UC17-R5: the copy of a subscription is always replaced by what Stripe says now, never patched from the event,
/// so repeated or out-of-order events cannot leave it wrong.
/// </summary>
public sealed class MembershipSync(IMembershipRepository memberships, IPaymentProvider payments, IUnitOfWork unitOfWork, TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task SyncAsync(Membership membership, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(membership);
        var before = membership.Status;
        var snapshot = await payments.GetSnapshotAsync(membership.CustomerId, cancellationToken).ConfigureAwait(false);
        membership.Apply(snapshot, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        // RNF7: the user only as the pseudonymous id, no amounts or card data.
        Logger.Info("Membership of {UserId} synced from Stripe: {Before} -> {After}", membership.OwnerId, before, membership.Status);
    }

    /// <summary>UC17 webhook: true when the event was about a MySync customer and the copy was refreshed.</summary>
    public async Task<bool> OnNotificationAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (notification.CustomerId is null
            || await memberships.GetByCustomerAsync(notification.CustomerId, cancellationToken).ConfigureAwait(false) is not { } membership)
        {
            // UC17-S9: customers of the Admin Portal (organizations) share the Stripe account.
            Logger.Debug("Stripe event {EventId} ({EventType}) is not about a MySync customer", notification.EventId, notification.Type);
            return false;
        }

        Logger.Info("Stripe event {EventId} ({EventType}) for {UserId}", notification.EventId, notification.Type, membership.OwnerId);
        await SyncAsync(membership, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>UC17-R4: the write gate of the API.</summary>
    public async Task<bool> CanWriteAsync(OwnerId ownerId, CancellationToken cancellationToken = default) =>
        (await memberships.GetAsync(ownerId, cancellationToken).ConfigureAwait(false))?.CanWrite == true;
}
