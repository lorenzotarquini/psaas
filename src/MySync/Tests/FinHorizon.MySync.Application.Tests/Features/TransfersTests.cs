using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Categories;
using FinHorizon.MySync.Application.Features.Dashboard;
using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Domain.Common;
using Xunit;

namespace FinHorizon.MySync.Application.Tests.Features;

/// <summary>UC9 at the application level.</summary>
public sealed class TransfersTests : IDisposable
{
    private readonly ApplicationHarness _app = new();

    [Fact]
    public async Task Transfer_MovesBalancesAndKeepsTheNetWorth()
    {
        var credem = _app.GivenAccount("Credem", 7600m);
        var revolut = _app.GivenAccount("Revolut", 23m);

        var result = await Transfer(new("Transfer", _app.Store.Today, 500m, credem.Id, null, null, null, revolut.Id));

        Assert.Equal(ResultStatus.Created, result.Status);
        Assert.Equal(("Credem", "Revolut"), (result.Value!.AccountName, result.Value.CounterAccountName));
        var dashboard = (await _app.SendAsync<GetDashboardQuery, DashboardView>(new())).Value!;
        Assert.Equal([7100m, 523m], dashboard.Accounts.Select(a => a.Balance));
        Assert.Equal(7623m, dashboard.NetWorth);
    }

    [Fact]
    public async Task Transfer_NeedsADestinationButNoCategory()
    {
        var credem = _app.GivenAccount("Credem");

        var missing = await Transfer(new("Transfer", _app.Store.Today, 10m, credem.Id, null, null, null));

        Assert.Equal([new FieldError("counterAccountId", ErrorCodes.Required)], missing.Errors);
    }

    [Fact]
    public async Task Expense_StillNeedsACategory()
    {
        var credem = _app.GivenAccount("Credem");

        var result = await Transfer(new("Expense", _app.Store.Today, 10m, credem.Id, null, null, null));

        Assert.Equal([new FieldError("categoryId", ErrorCodes.Required)], result.Errors);
    }

    [Fact]
    public async Task Category_OfTheTransferKindIsRefused()
    {
        var result = await _app.SendAsync<CreateCategoryCommand, CategoryView>(new("Transfer", "Giroconto"));

        Assert.Equal([new FieldError("kind", ErrorCodes.InvalidValue)], result.Errors);
    }

    [Fact]
    public async Task Totals_LeaveTransfersOut()
    {
        var credem = _app.GivenAccount("Credem", 1000m);
        var revolut = _app.GivenAccount("Revolut", 0m);
        await Transfer(new("Transfer", _app.Store.Today, 300m, credem.Id, null, null, null, revolut.Id));

        var page = (await _app.SendAsync<ListTransactionsQuery, TransactionPage>(new())).Value!;

        Assert.Equal((1, 0m, 0m), (page.TotalCount, page.ExpenseTotal, page.IncomeTotal));
    }

    private Task<Result<TransactionView>> Transfer(RecordTransactionCommand command) =>
        _app.SendAsync<RecordTransactionCommand, TransactionView>(command);

    public void Dispose() => _app.Dispose();
}
