using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Application.Abstractions.Persistence;

public interface IAccountRepository : IOwnedRepository<Account>
{
    Task<IReadOnlyList<Account>> ListAsync(OwnerId owner, CancellationToken cancellationToken = default);

    Task<int> CountAsync(OwnerId owner, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(OwnerId owner, string normalizedName, CancellationToken cancellationToken = default);
}
