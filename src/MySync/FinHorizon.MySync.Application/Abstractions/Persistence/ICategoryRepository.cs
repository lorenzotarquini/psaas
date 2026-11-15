using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;

namespace FinHorizon.MySync.Application.Abstractions.Persistence;

public interface ICategoryRepository : IOwnedRepository<Category>
{
    Task<Category?> FindByNameAsync(OwnerId owner, TransactionKind kind, string normalizedName, CancellationToken cancellationToken = default);

    /// <summary>Categories whose name contains the term, ordered by name (UC6 step 1).</summary>
    Task<IReadOnlyList<Category>> SearchAsync(OwnerId owner, TransactionKind? kind, string? term, int limit, CancellationToken cancellationToken = default);
}
