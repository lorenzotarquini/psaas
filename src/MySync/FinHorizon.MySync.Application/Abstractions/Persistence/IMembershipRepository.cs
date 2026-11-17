using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Memberships;

namespace FinHorizon.MySync.Application.Abstractions.Persistence;

/// <summary>UC17: the copy of each user's subscription to MySync.</summary>
public interface IMembershipRepository
{
    Task<Membership?> GetAsync(OwnerId ownerId, CancellationToken cancellationToken = default);

    Task<Membership?> GetByCustomerAsync(string customerId, CancellationToken cancellationToken = default);

    void Add(Membership membership);
}
