using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions;

/// <summary>
/// What the user enters for a movement, both when recording (UC5, UC9) and when amending (UC8).
/// <see cref="CategoryId"/> is for expenses and incomes, <see cref="CounterAccountId"/> (the destination) for transfers.
/// <see cref="CounterAmount"/> is what reaches the destination of a transfer between two currencies (UC13-R7).
/// <see cref="SubscriptionId"/> links an expense to the subscription it pays (UC16-R10).
/// </summary>
public sealed record TransactionData(
    TransactionKind Kind,
    DateOnly Date,
    Money Amount,
    Guid AccountId,
    Guid? CategoryId,
    Guid? DebtId,
    string? Note,
    Guid? CounterAccountId = null,
    Money? CounterAmount = null,
    Guid? SubscriptionId = null)
{
    public const int MaxNoteLength = 250;
}
