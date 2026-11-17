using System.Globalization;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Debts;
using FinHorizon.MySync.Domain.Projection;
using FinHorizon.MySync.Domain.Transactions;
using Xunit;
using static FinHorizon.MySync.Domain.Tests.Builders;

namespace FinHorizon.MySync.Domain.Tests.Projection;

/// <summary>Derived from UC14 (proiezione) and UC15 (spese fisse), plus the monthly payment of UC3-R8.</summary>
public sealed class ProjectionTests
{
    private static readonly DateOnly Day = new(2026, 11, 17);

    private static ProjectionParameters Parameters(
        int horizon = 12,
        decimal income = 2000m,
        decimal annualReturn = 2m,
        decimal inflation = 2m,
        IReadOnlyList<ProjectionEvent>? events = null,
        IReadOnlyList<ProjectionPayment>? payments = null,
        decimal? goal = null,
        IReadOnlyList<ProjectionSubscription>? subscriptions = null) =>
        new(horizon, income, 700m, 600m, 300m, annualReturn, inflation, false, payments ?? [], subscriptions ?? [], events ?? [], goal);

    [Theory]
    [InlineData("2026-01-10", 6, "2026-05-01", "2026-08-01", 3)]
    [InlineData("2026-09-15", 1, "2026-10-01", "2026-10-01", 1)]
    [InlineData("2026-09-01", 2, "2026-09-01", "2026-09-01", 2)]
    [InlineData("2026-11-02", 0, "2026-11-01", "2026-11-01", 0)]
    public void Window_AveragesTheCompleteMonthsAfterTheFirstAccount(string opening, int months, string from, string installmentsFrom, int installmentMonths)
    {
        var window = ProjectionWindow.For(Day, DateOnly.Parse(opening, CultureInfo.InvariantCulture));

        Assert.Equal(months, window.Months);
        Assert.Equal(DateOnly.Parse(from, CultureInfo.InvariantCulture), window.From);
        Assert.Equal(new DateOnly(2026, 10, 31), window.To);
        Assert.Equal(DateOnly.Parse(installmentsFrom, CultureInfo.InvariantCulture), window.InstallmentsFrom);
        Assert.Equal(installmentMonths, window.InstallmentMonths);
    }

    [Fact]
    public void Window_WithoutAccounts_IsEmpty() => Assert.True(ProjectionWindow.For(Day, null).IsEmpty);

    [Theory]
    [InlineData(1200, 6, 200)]
    [InlineData(100, 3, 33.33)]
    [InlineData(500, 0, 0)]
    public void Average_IsInCents(decimal total, int months, decimal expected) =>
        Assert.Equal(expected, ProjectionWindow.Average(total, months));

    [Fact]
    public void Settings_KeepWhatTheUserSaved()
    {
        var debt = Guid.NewGuid();
        var settings = ProjectionSettings.Save(Mario, Parameters(
            horizon: 60,
            events: [new ProjectionEvent(2, 1500m, " Tredicesima ")],
            payments: [new ProjectionPayment(debt, 250m)],
            goal: 30000m));

        Assert.Equal(60, settings.HorizonMonths);
        Assert.Equal("Tredicesima", Assert.Single(settings.Events).Note);
        Assert.Equal(debt, Assert.Single(settings.Payments).DebtId);
        Assert.Equal(30000m, settings.Goal);

        settings.Replace(Parameters());

        Assert.Empty(settings.Events);
        Assert.Null(settings.Goal);
    }

    public static TheoryData<ProjectionParameters> InvalidParameters => new()
    {
        Parameters(horizon: 0),
        Parameters(horizon: 361),
        Parameters(income: -1m),
        Parameters(income: 10.001m),
        Parameters(annualReturn: 25m),
        Parameters(annualReturn: -11m),
        Parameters(inflation: -1m),
        Parameters(goal: 0m),
        Parameters(events: Enumerable.Range(1, 11).Select(month => new ProjectionEvent(month, 10m, "x")).ToList()),
        Parameters(events: [new ProjectionEvent(13, 10m, "x")]),
        Parameters(events: [new ProjectionEvent(1, 0m, "x")]),
        Parameters(events: [new ProjectionEvent(1, 10m, " ")]),
        Parameters(events: [new ProjectionEvent(1, 10m, new string('x', 41))]),
        Parameters(payments: [new ProjectionPayment(Guid.Empty, -5m)]),
    };

    [Theory]
    [MemberData(nameof(InvalidParameters))]
    public void Settings_RejectWhatUC14R9Refuses(ProjectionParameters parameters) =>
        Assert.ThrowsAny<ArgumentException>(() => ProjectionSettings.Save(Mario, parameters));

    [Fact]
    public void Category_IsBornVariableAndAnExpenseCanBecomeFixed()
    {
        var category = Category();
        Assert.False(category.Fixed);

        category.SetFixed(true);

        Assert.True(category.Fixed);
    }

    [Fact]
    public void Category_OfIncomeIsNeverFixed()
    {
        var income = Domain.Categories.Category.Create(Mario, TransactionKind.Income, "Stipendio");

        Assert.Throws<InvalidOperationException>(() => income.SetFixed(true));
        income.SetFixed(false);
        Assert.False(income.Fixed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    [InlineData(10.001)]
    public void Debt_MonthlyPaymentMustBePositiveCents(decimal payment) =>
        Assert.Throws<ArgumentException>(() => Domain.Debts.Debt.Take(Mario, DebtKind.Loan, new Descriptor("Auto", null, null), Guid.NewGuid(), "EUR", Euro(4000m), Today, Euro(payment)));

    [Fact]
    public void Debt_MonthlyPaymentCanChangeAndBeRemoved()
    {
        var debt = Domain.Debts.Debt.Take(Mario, DebtKind.Loan, new Descriptor("Auto", null, null), Guid.NewGuid(), "EUR", Euro(4000m), Today, Euro(250m));
        Assert.Equal(Euro(250m), debt.MonthlyPayment);

        debt.Update(DebtKind.Loan, new Descriptor("Auto", null, null), debt.AccountId, "EUR", Euro(4000m), Money.Zero, Euro(300m));
        Assert.Equal(Euro(300m), debt.MonthlyPayment);

        debt.Update(DebtKind.Loan, new Descriptor("Auto", null, null), debt.AccountId, "EUR", Euro(4000m), Money.Zero);
        Assert.Null(debt.MonthlyPayment);
    }

    [Fact]
    public void Settings_KeepTheSubscriptions()
    {
        var netflix = Guid.NewGuid();
        var settings = ProjectionSettings.Save(Mario, Parameters(subscriptions: [new ProjectionSubscription(netflix, 17.99m, false)]));

        var saved = Assert.Single(settings.ToParameters().Subscriptions);
        Assert.Equal((netflix, 17.99m, false), (saved.SubscriptionId, saved.Amount, saved.Included));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10.001)]
    public void Settings_RefuseAWrongSubscriptionAmount(decimal amount) =>
        Assert.ThrowsAny<ArgumentException>(() => ProjectionSettings.Save(Mario, Parameters(subscriptions: [new ProjectionSubscription(Guid.NewGuid(), amount, true)])));

    [Fact]
    public void Settings_RefuseTheSameSubscriptionTwice()
    {
        var netflix = Guid.NewGuid();

        Assert.ThrowsAny<ArgumentException>(() => ProjectionSettings.Save(Mario, Parameters(subscriptions:
            [new ProjectionSubscription(netflix, 10m, true), new ProjectionSubscription(netflix, 12m, true)])));
    }
}
