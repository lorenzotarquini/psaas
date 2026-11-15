using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Dashboard;
using FinHorizon.MySync.Application.Features.NetWorth;
using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Domain.Common;
using Xunit;

namespace FinHorizon.MySync.Application.Tests.Features;

/// <summary>UC4, UC5, UC7 and UC8 at the application level.</summary>
public sealed class TransactionsTests : IDisposable
{
    private readonly ApplicationHarness _app = new();

    [Fact]
    public async Task Record_StoresAndReturnsTheNamedView()
    {
        var account = _app.GivenAccount();
        var category = _app.GivenCategory("Spesa alimentare");

        var result = await Record(new("Expense", _app.Store.Today, 85.40m, account.Id, category.Id, null, " pane "));

        Assert.Equal(ResultStatus.Created, result.Status);
        Assert.Equal("Conto Intesa", result.Value!.AccountName);
        Assert.Equal("Spesa alimentare", result.Value.CategoryName);
        Assert.Equal("pane", result.Value.Note);
        Assert.Single(_app.Store.Transactions);
    }

    [Theory]
    [InlineData("0", ErrorCodes.InvalidValue)]
    [InlineData("-10", ErrorCodes.InvalidValue)]
    [InlineData("10.005", ErrorCodes.InvalidFormat)]
    public async Task Record_AmountMustBePositiveCents(string amount, string code)
    {
        var account = _app.GivenAccount();

        var result = await Record(new("Expense", _app.Store.Today, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), account.Id, Guid.NewGuid(), null, null));

        Assert.Equal([new FieldError("amount", code)], result.Errors);
    }

    [Fact]
    public async Task Record_RulesOnStoredDataBlockTheSave()
    {
        var account = _app.GivenAccount();
        var annasCategory = _app.GivenCategory(owner: FakeStore.Anna);

        var result = await Record(new("Expense", _app.Store.Today.AddDays(1), 10m, account.Id, annasCategory.Id, null, null));

        Assert.Equal(["categoryId", "date"], result.Errors.Select(error => error.Field));
        Assert.Empty(_app.Store.Transactions);
        Assert.Equal(0, _app.Store.Saves);
    }

    [Fact]
    public async Task Amend_UnknownIsNotFound()
    {
        var result = await _app.SendAsync<AmendTransactionCommand, TransactionView>(
            new(Guid.NewGuid(), "Expense", _app.Store.Today, 1m, Guid.NewGuid(), Guid.NewGuid(), null, null));

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Theory]
    [InlineData("250", true)]
    [InlineData("300.01", false)]
    public async Task Amend_ChecksTheResidualWithoutItself(string amount, bool accepted)
    {
        var account = _app.GivenAccount();
        var category = _app.GivenCategory();
        var debt = _app.GivenDebt(account, residual: 300m);
        var installment = _app.GivenExpense(account, category, 300m, debt);

        var result = await _app.SendAsync<AmendTransactionCommand, TransactionView>(
            new(installment.Id, "Expense", _app.Store.Today, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), account.Id, category.Id, debt.Id, null));

        Assert.Equal(accepted, result.Succeeded);
        if (!accepted)
        {
            Assert.Equal([new FieldError("amount", ErrorCodes.InvalidValue)], result.Errors);
        }
    }

    [Fact]
    public async Task Amend_AnotherUsersMovementIsNotFound()
    {
        var annasAccount = _app.GivenAccount(owner: FakeStore.Anna);
        var annasMovement = _app.GivenExpense(annasAccount, _app.GivenCategory(owner: FakeStore.Anna), 10m);

        var result = await _app.SendAsync<AmendTransactionCommand, TransactionView>(
            new(annasMovement.Id, "Expense", _app.Store.Today, 1m, annasAccount.Id, annasMovement.CategoryId, null, null));

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal(10m, annasMovement.Amount.Amount);
    }

    [Fact]
    public async Task Delete_RemovesOrIsNotFound()
    {
        var movement = _app.GivenExpense(_app.GivenAccount(), _app.GivenCategory(), 10m);

        var deleted = await _app.SendAsync<DeleteTransactionCommand, Unit>(new(movement.Id));
        var again = await _app.SendAsync<DeleteTransactionCommand, Unit>(new(movement.Id));

        Assert.Equal(ResultStatus.Ok, deleted.Status);
        Assert.Equal(ResultStatus.NotFound, again.Status);
        Assert.Empty(_app.Store.Transactions);
    }

    [Fact]
    public async Task List_RejectsAReversedPeriodAndPageSizeOutOfRange()
    {
        var result = await _app.SendAsync<ListTransactionsQuery, TransactionPage>(
            new(From: _app.Store.Today, To: _app.Store.Today.AddDays(-1), PageSize: 101));

        Assert.Equal(["period", "pageSize"], result.Errors.Select(error => error.Field));
    }

    [Fact]
    public async Task Dashboard_SumsTheUsersPortfolio()
    {
        _app.Store.Limits = new(5, 5);
        var account = _app.GivenAccount(balance: 2500m);
        _app.GivenAccount("Contanti", balance: 200m);
        var debt = _app.GivenDebt(account, "Mutuo casa", 120_000m);
        _app.GivenExpense(account, _app.GivenCategory(), 650m, debt);
        _app.GivenAccount("Di Anna", 99_999m, FakeStore.Anna);

        var dashboard = (await _app.SendAsync<GetDashboardQuery, DashboardView>(new())).Value!;

        Assert.Equal(2050m, dashboard.NetWorth);
        Assert.Equal(119_350m, dashboard.DebtsResidual);
        Assert.Single(dashboard.RecentTransactions);
        Assert.Equal(new(5, 5), dashboard.Limits);
    }

    [Fact]
    public async Task NetWorth_UnknownPeriodIsInvalidAndMissingIs30Days()
    {
        _app.GivenAccount(openedDaysAgo: 100);

        var invalid = await _app.SendAsync<GetNetWorthTrendQuery, NetWorthTrendView>(new("7y"));
        var fallback = await _app.SendAsync<GetNetWorthTrendQuery, NetWorthTrendView>(new(null));

        Assert.Equal([new FieldError("period", ErrorCodes.InvalidValue)], invalid.Errors);
        Assert.Equal("30d", fallback.Value!.Period);
        Assert.Equal(30, fallback.Value.Points.Count);
    }

    private Task<Result<TransactionView>> Record(RecordTransactionCommand command) =>
        _app.SendAsync<RecordTransactionCommand, TransactionView>(command);

    public void Dispose() => _app.Dispose();
}
