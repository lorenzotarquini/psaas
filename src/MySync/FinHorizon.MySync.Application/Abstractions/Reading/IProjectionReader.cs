using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Application.Abstractions.Reading;

/// <summary>What a flow of money counts as in the suggested parameters of the projection (UC14-R3).</summary>
public enum ProjectionFlowKind
{
    Income = 0,

    /// <summary>An expense not linked to a debt, with a fixed category (UC15).</summary>
    FixedExpense = 1,

    /// <summary>An expense not linked to a debt, with a variable category.</summary>
    VariableExpense = 2,

    /// <summary>An expense linked to a debt: only the payment of that debt, never a fixed or variable expense (D17).</summary>
    Installment = 3,

    /// <summary>A transfer from a day-to-day account to an investment account (positive) or back (negative), D18.</summary>
    Saving = 4
}

/// <summary>The sum of one kind of flow on one day in one currency; <see cref="DebtId"/> only for installments.</summary>
public sealed record ProjectionFlow(DateOnly Date, string Currency, ProjectionFlowKind Kind, Guid? DebtId, decimal Amount);

/// <summary>The flows the suggested parameters of the projection average over (UC14-R3).</summary>
public interface IProjectionReader
{
    /// <summary>Flows of the user dated from <paramref name="from"/> to <paramref name="to"/>, both included; amounts positive except reverse savings.</summary>
    Task<IReadOnlyList<ProjectionFlow>> FlowsAsync(OwnerId owner, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
