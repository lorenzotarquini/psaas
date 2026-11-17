using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.NetWorth;
using FinHorizon.MySync.Domain.Preferences;
using FinHorizon.MySync.Domain.Subscriptions;
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

    public List<Subscription> Subscriptions { get; } = [];

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

    public Task<bool> NameExistsAsync(OwnerId owner, string normalizedName, Guid? exceptId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Of(owner).Any(account => account.Descriptor.NormalizedName == normalizedName && account.Id != exceptId));
}

internal sealed class FakeDebts(FakeStore store) : FakeRepository<Debt>(store.Debts), IDebtRepository
{
    public Task<IReadOnlyList<Debt>> ListAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Debt>>(Of(owner).ToList());

    public Task<int> CountAsync(OwnerId owner, CancellationToken cancellationToken = default) => Task.FromResult(Of(owner).Count());

    public Task<bool> NameExistsAsync(OwnerId owner, string normalizedName, Guid? exceptId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Of(owner).Any(debt => debt.Descriptor.NormalizedName == normalizedName && debt.Id != exceptId));
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

internal sealed class FakeSubscriptions(FakeStore store) : FakeRepository<Subscription>(store.Subscriptions), ISubscriptionRepository
{
    public Task<IReadOnlyList<Subscription>> ListAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Subscription>>(Of(owner).ToList());

    public Task<int> CountOpenAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        Task.FromResult(Of(owner).Count(subscription => !subscription.IsCancelled));

    public Task<bool> NameExistsAsync(OwnerId owner, string normalizedName, Guid? exceptId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Of(owner).Any(subscription => subscription.NormalizedName == normalizedName && subscription.Id != exceptId));

    public void Remove(Subscription subscription) => Items.Remove(subscription);
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
            .SelectMany(transaction => transaction.Postings)
            .GroupBy(posting => posting.AccountId)
            .ToDictionary(group => group.Key, group => Money.Sum(group.Select(posting => posting.Amount))));

    public Task<bool> HasMovementsAsync(OwnerId owner, Guid accountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Of(owner).Any(transaction => transaction.AccountId == accountId || transaction.CounterAccountId == accountId));

    public Task<IReadOnlyDictionary<Guid, Money>> InstallmentsByDebtAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, Money>>(Of(owner)
            .Where(transaction => transaction.DebtId is not null)
            .GroupBy(transaction => transaction.DebtId!.Value)
            .ToDictionary(group => group.Key, group => Money.Sum(group.Select(transaction => transaction.Amount))));

    public Task<Money> InstallmentsPaidAsync(OwnerId owner, Guid debtId, Guid? exceptTransactionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Money.Sum(Of(owner)
            .Where(transaction => transaction.DebtId == debtId && transaction.Id != exceptTransactionId)
            .Select(transaction => transaction.Amount)));

    public Task<IReadOnlyList<SubscriptionPayment>> SubscriptionPaymentsAsync(OwnerId owner, Guid? subscriptionId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SubscriptionPayment>>(Of(owner)
            .Where(transaction => transaction.SubscriptionId is not null && (subscriptionId is null || transaction.SubscriptionId == subscriptionId))
            .Select(transaction => new SubscriptionPayment(transaction.SubscriptionId!.Value, transaction.Date))
            .ToList());

    public Task<LedgerHistory> HistoryAsync(OwnerId owner, PortfolioScope scope, CancellationToken cancellationToken = default)
    {
        var accounts = store.Accounts.Where(a => a.OwnerId == owner && scope.Includes(a.Kind)).ToList();
        var debts = store.Debts.Where(d => d.OwnerId == owner && scope.Includes(d.Kind)).ToList();
        return Task.FromResult(new LedgerHistory(
            accounts.Select(a => new OpeningValue(a.OpenedOn, a.InitialBalance, a.Currency)).ToList(),
            debts.Select(d => new OpeningValue(d.CreatedOn, d.InitialResidual, d.Currency)).ToList(),
            Of(owner).GroupBy(t => t.Date).SelectMany(g => accounts.Select(a => a.Currency).Distinct().Select(currency => new DailyMovement(
                g.Key,
                Money.Sum(g.SelectMany(t => t.Postings).Where(p => accounts.Any(a => a.Id == p.AccountId && a.Currency == currency)).Select(p => p.Amount)),
                Money.Sum(g.Where(t => debts.Any(d => d.Id == t.DebtId && d.Currency == currency)).Select(t => t.Amount)),
                currency))).ToList()));
    }

    public Task<TransactionView?> GetAsync(OwnerId owner, Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Of(owner).Where(transaction => transaction.Id == id).Select(View).SingleOrDefault());

    public Task<TransactionSlice> ListAsync(OwnerId owner, TransactionFilter filter, CancellationToken cancellationToken = default)
    {
        var all = Of(owner).OrderByDescending(t => t.Date).ThenByDescending(t => t.RecordedAt).ToList();
        var items = all.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).Select(View).ToList();
        var totals = all
            .GroupBy(t => (t.Date, CurrencyOf(t.AccountId)))
            .Select(g => new DailyTotal(
                g.Key.Date,
                g.Key.Item2,
                g.Where(t => t.Kind == TransactionKind.Expense).Sum(t => t.Amount.Amount),
                g.Where(t => t.Kind == TransactionKind.Income).Sum(t => t.Amount.Amount)))
            .ToList();
        return Task.FromResult(new TransactionSlice(items, all.Count, totals));
    }

    private string CurrencyOf(Guid accountId) => store.Accounts.Single(a => a.Id == accountId).Currency;

    private IEnumerable<Transaction> Of(OwnerId owner) => store.Transactions.Where(transaction => transaction.OwnerId == owner);

    private TransactionView View(Transaction t) => new(
        t.Id,
        t.Kind,
        t.Date,
        t.Amount.Amount,
        t.AccountId,
        store.Accounts.Single(a => a.Id == t.AccountId).Descriptor.Name,
        t.CategoryId,
        store.Categories.SingleOrDefault(c => c.Id == t.CategoryId)?.Name,
        t.DebtId,
        store.Debts.SingleOrDefault(d => d.Id == t.DebtId)?.Descriptor.Name,
        t.Note,
        t.RecordedAt,
        t.CounterAccountId,
        store.Accounts.SingleOrDefault(a => a.Id == t.CounterAccountId)?.Descriptor.Name,
        CurrencyOf(t.AccountId),
        t.CounterAmount?.Amount,
        store.Accounts.SingleOrDefault(a => a.Id == t.CounterAccountId)?.Currency,
        t.SubscriptionId,
        store.Subscriptions.SingleOrDefault(s => s.Id == t.SubscriptionId)?.Descriptor.Name);
}

/// <summary>Stored preferences, one per user.</summary>
internal sealed class FakePreferences : IPreferencesRepository
{
    private readonly List<UserPreferences> _items = [];

    public Task<UserPreferences?> GetAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.SingleOrDefault(item => item.OwnerId == owner));

    public void Add(UserPreferences preferences) => _items.Add(preferences);
}

/// <summary>
/// The currency service of UC13 with fixed values: 1 EUR = 1.10 USD, 1 BTC = 50,000 EUR, 1 ETH = 3,000 EUR.
/// Rates of single days can be set, and the service can be switched off (UC13-5a).
/// </summary>
internal sealed class FakeCurrencies : ICurrencyCatalog, IExchangeRates
{
    private static readonly Dictionary<string, decimal> ValueInEuro = new()
    {
        ["EUR"] = 1m,
        ["USD"] = 1m / 1.1m,
        ["GBP"] = 1.15m,
        ["BTC"] = 50_000m,
        ["ETH"] = 3_000m,
    };

    public List<DailyRate> DayRates { get; } = [];

    public bool Unavailable { get; set; }

    public Task<IReadOnlyList<CurrencyInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        return Task.FromResult<IReadOnlyList<CurrencyInfo>>(
        [
            new("EUR", "Euro", "€", CurrencyType.Fiat),
            new("USD", "United States Dollar", "$", CurrencyType.Fiat),
            new("GBP", "British Pound", "£", CurrencyType.Fiat),
            new("BTC", "Bitcoin", "₿", CurrencyType.Crypto),
            new("ETH", "Ethereum", "Ξ", CurrencyType.Crypto),
        ]);
    }

    public Task<IReadOnlyList<DailyRate>> SeriesAsync(string currency, string displayCurrency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        var days = DayRates.Where(rate => rate.Currency == currency && rate.Date >= from && rate.Date <= to).ToList();
        if (days.Count > 0 || !ValueInEuro.TryGetValue(currency, out var value) || !ValueInEuro.TryGetValue(displayCurrency, out var display))
        {
            return Task.FromResult<IReadOnlyList<DailyRate>>(days);
        }

        return Task.FromResult<IReadOnlyList<DailyRate>>([new DailyRate(currency, from, value / display)]);
    }

    private void ThrowIfUnavailable()
    {
        if (Unavailable)
        {
            throw new CurrencyServiceUnavailableException("off");
        }
    }
}

/// <summary>The flows of the projection, set by the test as they would come out of the database.</summary>
internal sealed class FakeProjectionReader : IProjectionReader
{
    public List<ProjectionFlow> Flows { get; } = [];

    public Task<IReadOnlyList<ProjectionFlow>> FlowsAsync(OwnerId owner, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProjectionFlow>>(Flows.Where(flow => flow.Date >= from && flow.Date <= to).ToList());
}

internal sealed class FakeProjections : IProjectionRepository
{
    public List<Domain.Projection.ProjectionSettings> Items { get; } = [];

    public Task<Domain.Projection.ProjectionSettings?> GetAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(item => item.OwnerId == owner));

    public void Add(Domain.Projection.ProjectionSettings settings) => Items.Add(settings);

    public void Remove(Domain.Projection.ProjectionSettings settings) => Items.Remove(settings);
}
