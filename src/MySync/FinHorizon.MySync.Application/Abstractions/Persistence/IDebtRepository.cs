using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Debts;

namespace FinHorizon.MySync.Application.Abstractions.Persistence;

public interface IDebtRepository : IOwnedRepository<Debt>
{
    Task<IReadOnlyList<Debt>> ListAsync(OwnerId owner, CancellationToken cancellationToken = default);

    /// <summary>Paid-off debts included (UC3-R6).</summary>
    Task<int> CountAsync(OwnerId owner, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(OwnerId owner, string normalizedName, CancellationToken cancellationToken = default);
}
