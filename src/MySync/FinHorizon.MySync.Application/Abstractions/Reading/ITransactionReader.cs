using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Application.Abstractions.Reading;

/// <summary>Movements as the user sees them, with the names of what they point to.</summary>
public interface ITransactionReader
{
    Task<TransactionView?> GetAsync(OwnerId owner, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Most recent first (by date, then by entry), filtered and paged, with the totals of the whole filter (UC8).</summary>
    Task<TransactionSlice> ListAsync(OwnerId owner, TransactionFilter filter, CancellationToken cancellationToken = default);
}
