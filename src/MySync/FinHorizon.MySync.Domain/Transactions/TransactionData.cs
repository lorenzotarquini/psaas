using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions;

/// <summary>
/// What the user enters for a movement, both when recording (UC5, UC9) and when amending (UC8).
/// <see cref="CategoryId"/> is for expenses and incomes, <see cref="CounterAccountId"/> (the destination) for transfers.
/// </summary>
public sealed record TransactionData(
    TransactionKind Kind,
    DateOnly Date,
    Money Amount,
    Guid AccountId,
    Guid? CategoryId,
    Guid? DebtId,
    string? Note,
    Guid? CounterAccountId = null)
{
    public const int MaxNoteLength = 250;
}
