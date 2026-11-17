using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Subscriptions;

namespace FinHorizon.MySync.Application.Abstractions.Persistence;

public interface ISubscriptionRepository : IOwnedRepository<Subscription>
{
    Task<IReadOnlyList<Subscription>> ListAsync(OwnerId owner, CancellationToken cancellationToken = default);

    /// <summary>The subscriptions without an end date: the only ones in the limit (UC16-R4).</summary>
    Task<int> CountOpenAsync(OwnerId owner, CancellationToken cancellationToken = default);

    /// <summary>Whether another subscription of the owner has the name; <paramref name="exceptId"/> is the one being renamed.</summary>
    Task<bool> NameExistsAsync(OwnerId owner, string normalizedName, Guid? exceptId = null, CancellationToken cancellationToken = default);

    void Remove(Subscription subscription);
}
