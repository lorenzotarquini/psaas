using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace FinHorizon.MySync.Application.Tests;

/// <summary>The real application composition (handlers, decorators, rules) on top of <see cref="FakeStore"/>.</summary>
internal sealed class ApplicationHarness : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public ApplicationHarness()
    {
        var services = new ServiceCollection();
        services.AddMySyncApplication();
        services.AddSingleton(Store);
        services.AddSingleton<IUnitOfWork>(Store);
        services.AddSingleton<ICurrentUser>(Store);
        services.AddSingleton<ICalendar>(Store);
        services.AddSingleton<IResourceLimits>(Store);
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 11, 15, 9, 0, 0, TimeSpan.Zero)) { AutoAdvanceAmount = TimeSpan.FromSeconds(1) });
        services.AddSingleton<IAccountRepository>(new FakeAccounts(Store));
        services.AddSingleton<IDebtRepository>(new FakeDebts(Store));
        services.AddSingleton<ICategoryRepository>(new FakeCategories(Store));
        services.AddSingleton<ITransactionRepository>(new FakeTransactions(Store));
        var ledger = new FakeLedger(Store);
        services.AddSingleton<ILedgerReader>(ledger);
        services.AddSingleton<ITransactionReader>(ledger);
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        _scope = _provider.CreateScope();
    }

    public FakeStore Store { get; } = new();

    public IRequestHandler<TRequest, TResponse> Handler<TRequest, TResponse>()
        where TRequest : notnull =>
        _scope.ServiceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

    public Task<Result<TResponse>> SendAsync<TRequest, TResponse>(TRequest request)
        where TRequest : notnull =>
        Handler<TRequest, TResponse>().HandleAsync(request);

    // ---- Given ----

    public Account GivenAccount(string name = "Conto Intesa", decimal balance = 1000m, OwnerId? owner = null, int openedDaysAgo = 30)
    {
        var account = Account.Open(owner ?? Store.Id, AccountKind.CurrentAccount, new Descriptor(name, null, null), new Money(balance), Store.Today.AddDays(-openedDaysAgo));
        Store.Accounts.Add(account);
        return account;
    }

    public Debt GivenDebt(Account account, string name = "Auto", decimal residual = 300m)
    {
        var debt = Debt.Take(account.OwnerId, DebtKind.Loan, new Descriptor(name, null, null), account.Id, new Money(residual), account.OpenedOn);
        Store.Debts.Add(debt);
        return debt;
    }

    public Category GivenCategory(string name = "Spesa", TransactionKind kind = TransactionKind.Expense, OwnerId? owner = null)
    {
        var category = Category.Create(owner ?? Store.Id, kind, name);
        Store.Categories.Add(category);
        return category;
    }

    public Transaction GivenExpense(Account account, Category category, decimal amount, Debt? debt = null)
    {
        var transaction = Transaction.Record(
            account.OwnerId,
            new TransactionData(TransactionKind.Expense, Store.Today, new Money(amount), account.Id, category.Id, debt?.Id, null),
            DateTimeOffset.UtcNow);
        Store.Transactions.Add(transaction);
        return transaction;
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
