using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions;

/// <summary>What the user enters for a movement, both when recording (UC5) and when amending (UC8).</summary>
public sealed record TransactionData(
    TransactionKind Kind,
    DateOnly Date,
    Money Amount,
    Guid AccountId,
    Guid CategoryId,
    Guid? DebtId,
    string? Note)
{
    public const int MaxNoteLength = 250;
}
