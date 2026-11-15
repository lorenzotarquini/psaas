using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Application.Abstractions.Persistence;

public interface IAccountRepository : IOwnedRepository<Account>
{
    Task<IReadOnlyList<Account>> ListAsync(OwnerId owner, CancellationToken cancellationToken = default);

    Task<int> CountAsync(OwnerId owner, CancellationToken cancellationToken = default);

    /// <summary>Whether another record of the owner has the name; <paramref name="exceptId"/> is the one being renamed (UC10).</summary>
    Task<bool> NameExistsAsync(OwnerId owner, string normalizedName, Guid? exceptId = null, CancellationToken cancellationToken = default);
}
