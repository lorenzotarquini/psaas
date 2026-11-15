using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.Transactions;
using FinHorizon.MySync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinHorizon.MySync.Infrastructure.Persistence;

internal sealed class AccountRepository(MySyncDbContext db) : OwnedRepository<Account>(db), IAccountRepository
{
    public async Task<IReadOnlyList<Account>> ListAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        await Of(owner).AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<int> CountAsync(OwnerId owner, CancellationToken cancellationToken = default) => Of(owner).CountAsync(cancellationToken);

    public Task<bool> NameExistsAsync(OwnerId owner, string normalizedName, CancellationToken cancellationToken = default) =>
        Of(owner).AnyAsync(account => account.NormalizedName == normalizedName, cancellationToken);
}

internal sealed class DebtRepository(MySyncDbContext db) : OwnedRepository<Debt>(db), IDebtRepository
{
    public async Task<IReadOnlyList<Debt>> ListAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        await Of(owner).AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<int> CountAsync(OwnerId owner, CancellationToken cancellationToken = default) => Of(owner).CountAsync(cancellationToken);

    public Task<bool> NameExistsAsync(OwnerId owner, string normalizedName, CancellationToken cancellationToken = default) =>
        Of(owner).AnyAsync(debt => debt.NormalizedName == normalizedName, cancellationToken);
}

internal sealed class CategoryRepository(MySyncDbContext db) : OwnedRepository<Category>(db), ICategoryRepository
{
    public Task<Category?> FindByNameAsync(OwnerId owner, TransactionKind kind, string normalizedName, CancellationToken cancellationToken = default) =>
        Of(owner).AsNoTracking().SingleOrDefaultAsync(category => category.Kind == kind && category.NormalizedName == normalizedName, cancellationToken);

    public async Task<IReadOnlyList<Category>> SearchAsync(OwnerId owner, TransactionKind? kind, string? term, int limit, CancellationToken cancellationToken = default)
    {
        var query = Of(owner).AsNoTracking();
        if (kind is not null)
        {
            query = query.Where(category => category.Kind == kind);
        }

        if (term is not null)
        {
            var normalizedTerm = TextNormalizer.Normalize(term);
            query = query.Where(category => category.NormalizedName.Contains(normalizedTerm));
        }

        return await query.OrderBy(category => category.Name).Take(limit).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class TransactionRepository(MySyncDbContext db) : OwnedRepository<Transaction>(db), ITransactionRepository
{
    public void Remove(Transaction transaction) => Db.Transactions.Remove(transaction);
}
