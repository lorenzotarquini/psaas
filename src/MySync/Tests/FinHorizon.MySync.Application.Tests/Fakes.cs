using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.NetWorth;
using FinHorizon.MySync.Domain.Transactions;

namespace FinHorizon.MySync.Application.Tests;

/// <summary>An in-memory MySync: what the database would hold, plus the knobs the scenarios need.</summary>
internal sealed class FakeStore : IUnitOfWork, ICurrentUser, ICalendar, IResourceLimits
{
    public static readonly OwnerId Mario = new("kc-mario");
    public static readonly OwnerId Anna = new("kc-anna");

    public List<Account> Accounts { get; } = [];

    public List<Debt> Debts { get; } = [];

    public List<Category> Categories { get; } = [];

    public List<Transaction> Transactions { get; } = [];

    public OwnerId Id { get; set; } = Mario;

    public DateOnly Today { get; set; } = new(2026, 11, 15);

    public UserLimits Limits { get; set; } = new(2, 1);

    public int Saves { get; private set; }

    /// <summary>Simulates another request storing the same unique name first.</summary>
    public bool FailNextSaveWithDuplicate { get; set; }

    public Action? BeforeDuplicateFailure { get; set; }

    public Task<UserLimits> ForAsync(OwnerId owner, CancellationToken cancellationToken = default) => Task.FromResult(Limits);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (FailNextSaveWithDuplicate)
        {
            FailNextSaveWithDuplicate = false;
            BeforeDuplicateFailure?.Invoke();
            throw new DuplicateNameException("duplicate");
        }

        Saves++;
        return Task.CompletedTask;
    }
}

internal class FakeRepository<T>(List<T> items) : IOwnedRepository<T>
    where T : Entity
{
    protected List<T> Items => items;

    public Task<T?> GetAsync(OwnerId owner, Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(items.SingleOrDefault(item => item.OwnerId == owner && item.Id == id));

    public void Add(T entity) => items.Add(entity);

    protected IEnumerable<T> Of(OwnerId owner) => items.Where(item => item.OwnerId == owner);
}

internal sealed class FakeAccounts(FakeStore store) : FakeRepository<Account>(store.Accounts), IAccountRepository
{
    public Task<IReadOnlyList<Account>> ListAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Account>>(Of(owner).ToList());

    public Task<int> CountAsync(OwnerId owner, CancellationToken cancellationToken = default) => Task.FromResult(Of(owner).Count());

    public Task<bool> NameExistsAsync(OwnerId owner, string normalizedName, CancellationToken cancellationToken = default) =>
        Task.FromResult(Of(owner).Any(account => account.Descriptor.NormalizedName == normalizedName));
}

internal sealed class FakeDebts(FakeStore store) : FakeRepository<Debt>(store.Debts), IDebtRepository
{
    public Task<IReadOnlyList<Debt>> ListAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Debt>>(Of(owner).ToList());

    public Task<int> CountAsync(OwnerId owner, CancellationToken cancellationToken = default) => Task.FromResult(Of(owner).Count());

    public Task<bool> NameExistsAsync(OwnerId owner, string normalizedName, CancellationToken cancellationToken = default) =>
        Task.FromResult(Of(owner).Any(debt => debt.Descriptor.NormalizedName == normalizedName));
}

internal sealed class FakeCategories(FakeStore store) : FakeRepository<Category>(store.Categories), ICategoryRepository
{
    public Task<Category?> FindByNameAsync(OwnerId owner, TransactionKind kind, string normalizedName, CancellationToken cancellationToken = default) =>
        Task.FromResult(Of(owner).SingleOrDefault(category => category.Kind == kind && category.NormalizedName == normalizedName));

    public Task<IReadOnlyList<Category>> SearchAsync(OwnerId owner, TransactionKind? kind, string? term, int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Category>>(Of(owner)
            .Where(category => kind is null || category.Kind == kind)
            .Where(category => term is null || category.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(category => category.Name)
            .Take(limit)
            .ToList());
}

internal sealed class FakeTransactions(FakeStore store) : FakeRepository<Transaction>(store.Transactions), ITransactionRepository
{
    public void Remove(Transaction transaction) => Items.Remove(transaction);
}

/// <summary>The sums the database would compute, over the in-memory movements.</summary>
internal sealed class FakeLedger(FakeStore store) : ILedgerReader, ITransactionReader
{
    public Task<IReadOnlyDictionary<Guid, Money>> MovementsByAccountAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, Money>>(Of(owner)
            .GroupBy(transaction => transaction.AccountId)
            .ToDictionary(group => group.Key, group => Money.Sum(group.Select(transaction => transaction.SignedAmount))));

    public Task<IReadOnlyDictionary<Guid, Money>> InstallmentsByDebtAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, Money>>(Of(owner)
            .Where(transaction => transaction.DebtId is not null)
            .GroupBy(transaction => transaction.DebtId!.Value)
            .ToDictionary(group => group.Key, group => Money.Sum(group.Select(transaction => transaction.Amount))));

    public Task<Money> InstallmentsPaidAsync(OwnerId owner, Guid debtId, Guid? exceptTransactionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Money.Sum(Of(owner)
            .Where(transaction => transaction.DebtId == debtId && transaction.Id != exceptTransactionId)
            .Select(transaction => transaction.Amount)));

    public Task<LedgerHistory> HistoryAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        Task.FromResult(new LedgerHistory(
            store.Accounts.Where(a => a.OwnerId == owner).Select(a => new OpeningValue(a.OpenedOn, a.InitialBalance)).ToList(),
            store.Debts.Where(d => d.OwnerId == owner).Select(d => new OpeningValue(d.CreatedOn, d.InitialResidual)).ToList(),
            Of(owner).GroupBy(t => t.Date).Select(g => new DailyMovement(
                g.Key,
                Money.Sum(g.Select(t => t.SignedAmount)),
                Money.Sum(g.Where(t => t.DebtId is not null).Select(t => t.Amount)))).ToList()));

    public Task<TransactionView?> GetAsync(OwnerId owner, Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Of(owner).Where(transaction => transaction.Id == id).Select(View).SingleOrDefault());

    public Task<TransactionPage> ListAsync(OwnerId owner, TransactionFilter filter, CancellationToken cancellationToken = default)
    {
        var all = Of(owner).OrderByDescending(t => t.Date).ThenByDescending(t => t.RecordedAt).ToList();
        var items = all.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).Select(View).ToList();
        return Task.FromResult(new TransactionPage(
            items,
            filter.Page,
            filter.PageSize,
            all.Count,
            all.Where(t => t.Kind == TransactionKind.Expense).Sum(t => t.Amount.Amount),
            all.Where(t => t.Kind == TransactionKind.Income).Sum(t => t.Amount.Amount)));
    }

    private IEnumerable<Transaction> Of(OwnerId owner) => store.Transactions.Where(transaction => transaction.OwnerId == owner);

    private TransactionView View(Transaction t) => new(
        t.Id,
        t.Kind,
        t.Date,
        t.Amount.Amount,
        t.AccountId,
        store.Accounts.Single(a => a.Id == t.AccountId).Descriptor.Name,
        t.CategoryId,
        store.Categories.Single(c => c.Id == t.CategoryId).Name,
        t.DebtId,
        store.Debts.SingleOrDefault(d => d.Id == t.DebtId)?.Descriptor.Name,
        t.Note,
        t.RecordedAt);
}
