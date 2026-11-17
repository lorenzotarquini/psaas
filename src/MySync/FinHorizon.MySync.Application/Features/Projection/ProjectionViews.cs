using FinHorizon.MySync.Domain.Projection;

namespace FinHorizon.MySync.Application.Features.Projection;

/// <summary>UC14 step 2: today's balances by group, in the display currency.</summary>
public sealed record ProjectionStartView(decimal Liquidity, decimal Investments, decimal Stable);

/// <summary>An active debt: its residual and the monthly payment the user gave (UC3-R8), both in the display currency.</summary>
public sealed record ProjectionDebtView(Guid Id, string Name, decimal Residual, decimal? MonthlyPayment);

public sealed record ProjectionPaymentView(Guid DebtId, decimal Amount);

public sealed record ProjectionEventView(int Month, decimal Amount, string Note);

/// <summary>UC14-R3: the parameters proposed from the movements.</summary>
public sealed record SuggestedParametersView(
    decimal Income,
    decimal FixedExpenses,
    decimal VariableExpenses,
    decimal Saving,
    decimal AnnualReturn,
    decimal Inflation,
    IReadOnlyList<ProjectionPaymentView> Payments);

/// <summary>UC14 step 10: what the user saved, the same shape as the body of PUT /api/projection.</summary>
public sealed record SavedProjectionView(
    int HorizonMonths,
    decimal Income,
    decimal FixedExpenses,
    decimal VariableExpenses,
    decimal Saving,
    decimal AnnualReturn,
    decimal Inflation,
    bool InflationOn,
    IReadOnlyList<ProjectionPaymentView> Payments,
    IReadOnlyList<ProjectionEventView> Events,
    decimal? Goal)
{
    public static SavedProjectionView From(ProjectionSettings settings, IReadOnlySet<Guid>? activeDebts = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new SavedProjectionView(
            settings.HorizonMonths,
            settings.Income,
            settings.FixedExpenses,
            settings.VariableExpenses,
            settings.Saving,
            settings.AnnualReturn,
            settings.Inflation,
            settings.InflationOn,
            // UC14-R11: the payment of a debt that is gone is ignored.
            settings.Payments
                .Where(payment => activeDebts is null || activeDebts.Contains(payment.DebtId))
                .Select(payment => new ProjectionPaymentView(payment.DebtId, payment.Amount))
                .ToList(),
            settings.Events.Select(item => new ProjectionEventView(item.Month, item.Amount, item.Note)).ToList(),
            settings.Goal);
    }
}

/// <summary>
/// Everything the frontend needs to compute the projection (UC14-R10), in <see cref="Currency"/>. <see cref="Today"/> is the
/// backend's today (RNF4): month 1 is the calendar month after it (UC14-R2).
/// </summary>
public sealed record ProjectionView(
    DateOnly Today,
    ProjectionStartView Start,
    IReadOnlyList<ProjectionDebtView> Debts,
    SuggestedParametersView Suggested,
    int MonthsOfData,
    SavedProjectionView? Saved,
    string Currency);
