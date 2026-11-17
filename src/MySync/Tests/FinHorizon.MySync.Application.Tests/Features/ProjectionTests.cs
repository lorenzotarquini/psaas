using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Categories;
using FinHorizon.MySync.Application.Features.Projection;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;
using Xunit;

namespace FinHorizon.MySync.Application.Tests.Features;

/// <summary>UC14 and UC15 at the application level; the store's today is 2026-11-15.</summary>
public sealed class ProjectionTests : IDisposable
{
    private readonly ApplicationHarness _app = new();

    private static SaveProjectionCommand Command(
        int? horizon = 12,
        decimal? income = 2000m,
        IReadOnlyList<ProjectionPaymentInput>? payments = null,
        IReadOnlyList<ProjectionEventInput>? events = null,
        decimal? annualReturn = 2m,
        decimal? goal = null) =>
        new(horizon, income, 700m, 600m, 300m, annualReturn, 2m, false, payments, events, goal);

    private void Flow(ProjectionFlowKind kind, decimal amount, int month, Guid? debtId = null) =>
        _app.ProjectionReader.Flows.Add(new ProjectionFlow(new DateOnly(2026, month, 10), "EUR", kind, debtId, amount));

    [Fact]
    public async Task Get_StartsFromTodayAndAveragesTheLastSixMonths()
    {
        _app.GivenAccount("Conto Intesa", 3000m, openedDaysAgo: 300);
        _app.GivenAccount("Deposito", 5000m, kind: AccountKind.DepositAccount);
        _app.GivenAccount("Oro", 2000m, kind: AccountKind.Valuables);
        var auto = _app.GivenDebt(_app.Store.Accounts[0], "Auto", 4000m);
        for (var month = 5; month <= 10; month++)
        {
            Flow(ProjectionFlowKind.Income, 2000m, month);
            Flow(ProjectionFlowKind.FixedExpense, 700m, month);
            Flow(ProjectionFlowKind.VariableExpense, 600m, month);
            Flow(ProjectionFlowKind.Installment, 250m, month, auto.Id);
            Flow(ProjectionFlowKind.Saving, 300m, month);
        }

        var view = (await _app.SendAsync<GetProjectionQuery, ProjectionView>(new())).Value!;

        Assert.Equal(new ProjectionStartView(3000m, 5000m, 2000m), view.Start);
        Assert.Equal(6, view.MonthsOfData);
        Assert.Equal((2000m, 700m, 600m, 300m), (view.Suggested.Income, view.Suggested.FixedExpenses, view.Suggested.VariableExpenses, view.Suggested.Saving));
        Assert.Equal(new ProjectionPaymentView(auto.Id, 250m), Assert.Single(view.Suggested.Payments));
        Assert.Equal(new ProjectionDebtView(auto.Id, "Auto", 4000m, null), Assert.Single(view.Debts));
        Assert.Equal((2m, 2m), (view.Suggested.AnnualReturn, view.Suggested.Inflation));
        Assert.Null(view.Saved);
    }

    [Fact]
    public async Task Get_SavingNeverGoesBelowZero()
    {
        _app.GivenAccount(openedDaysAgo: 300);
        Flow(ProjectionFlowKind.Saving, -600m, 9);

        var view = (await _app.SendAsync<GetProjectionQuery, ProjectionView>(new())).Value!;

        Assert.Equal(0m, view.Suggested.Saving);
    }

    [Fact]
    public async Task Get_WithoutWholeMonths_ProposesZero()
    {
        _app.GivenAccount(openedDaysAgo: 5);

        var view = (await _app.SendAsync<GetProjectionQuery, ProjectionView>(new())).Value!;

        Assert.Equal(0, view.MonthsOfData);
        Assert.Equal(0m, view.Suggested.Income);
    }

    [Fact]
    public async Task SaveThenGet_ReturnsTheSavedValuesAndDropsPaymentsOfDebtsThatAreGone()
    {
        var account = _app.GivenAccount(openedDaysAgo: 300);
        var auto = _app.GivenDebt(account, "Auto", 4000m);
        var saved = await _app.SendAsync<SaveProjectionCommand, SavedProjectionView>(Command(
            horizon: 60,
            payments: [new ProjectionPaymentInput(auto.Id, 300m)],
            events: [new ProjectionEventInput(2, 1500m, "Tredicesima")],
            goal: 30000m));
        Assert.Equal(ResultStatus.Ok, saved.Status);

        _app.Store.Debts.Clear();
        var view = (await _app.SendAsync<GetProjectionQuery, ProjectionView>(new())).Value!;

        Assert.Equal(60, view.Saved!.HorizonMonths);
        Assert.Equal(30000m, view.Saved.Goal);
        Assert.Equal(new ProjectionEventView(2, 1500m, "Tredicesima"), Assert.Single(view.Saved.Events));
        Assert.Empty(view.Saved.Payments);
    }

    [Fact]
    public async Task Save_TwiceReplacesTheOneProjection()
    {
        _app.GivenAccount();
        await _app.SendAsync<SaveProjectionCommand, SavedProjectionView>(Command(horizon: 60));
        await _app.SendAsync<SaveProjectionCommand, SavedProjectionView>(Command(horizon: 24));

        Assert.Equal(24, Assert.Single(_app.Projections.Items).HorizonMonths);
    }

    [Fact]
    public async Task Delete_RemovesTheSavedProjectionAndIsFineWithoutOne()
    {
        _app.GivenAccount();
        await _app.SendAsync<SaveProjectionCommand, SavedProjectionView>(Command());

        Assert.Equal(ResultStatus.Ok, (await _app.SendAsync<DeleteProjectionCommand, Unit>(new())).Status);
        Assert.Empty(_app.Projections.Items);
        Assert.Equal(ResultStatus.Ok, (await _app.SendAsync<DeleteProjectionCommand, Unit>(new())).Status);
    }

    public static TheoryData<SaveProjectionCommand, string, string> InvalidCommands => new()
    {
        { Command(horizon: 0), "horizonMonths", ErrorCodes.InvalidValue },
        { Command(horizon: 361), "horizonMonths", ErrorCodes.InvalidValue },
        { Command(income: -1m), "income", ErrorCodes.InvalidValue },
        { Command(income: 10.001m), "income", ErrorCodes.InvalidFormat },
        { Command(annualReturn: 25m), "annualReturn", ErrorCodes.InvalidValue },
        { Command(goal: 0m), "goal", ErrorCodes.InvalidValue },
        { Command(events: Enumerable.Range(1, 11).Select(month => new ProjectionEventInput(month, 10m, "x")).ToList()), "events", ErrorCodes.InvalidValue },
        { Command(events: [new ProjectionEventInput(13, 10m, "x")]), "events[0].month", ErrorCodes.InvalidValue },
        { Command(events: [new ProjectionEventInput(1, 0m, "x")]), "events[0].amount", ErrorCodes.InvalidValue },
        { Command(events: [new ProjectionEventInput(1, 10m, null)]), "events[0].note", ErrorCodes.Required },
        { Command(payments: [new ProjectionPaymentInput(Guid.NewGuid(), 10m)]), "payments[0].debtId", ErrorCodes.InvalidValue },
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Save_RejectsWhatUC14R9Refuses(SaveProjectionCommand command, string field, string code)
    {
        _app.GivenAccount();

        var result = await _app.SendAsync<SaveProjectionCommand, SavedProjectionView>(command);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.Errors, error => error.Field == field && error.Code == code);
        Assert.Empty(_app.Projections.Items);
    }

    [Fact]
    public async Task SetFixed_MakesAnExpenseFixed()
    {
        var rent = _app.GivenCategory("Affitto");

        var result = await _app.SendAsync<SetCategoryFixedCommand, CategoryView>(new(rent.Id, true));

        Assert.True(result.Value!.Fixed);
        Assert.True(rent.Fixed);
    }

    [Fact]
    public async Task SetFixed_RefusesIncomeAndOtherUsers()
    {
        var salary = _app.GivenCategory("Stipendio", TransactionKind.Income);
        var annas = _app.GivenCategory("Affitto", owner: new OwnerId("kc-anna"));

        var income = await _app.SendAsync<SetCategoryFixedCommand, CategoryView>(new(salary.Id, true));
        var other = await _app.SendAsync<SetCategoryFixedCommand, CategoryView>(new(annas.Id, true));

        Assert.Equal(new FieldError("fixed", ErrorCodes.InvalidValue), Assert.Single(income.Errors));
        Assert.Equal(ResultStatus.NotFound, other.Status);
        Assert.False(annas.Fixed);
    }

    public void Dispose() => _app.Dispose();
}
