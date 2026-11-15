namespace FinHorizon.MySync.Domain.Transactions;

/// <summary>The kinds of movement; what each one means is in <see cref="Kinds.MovementKinds"/>.</summary>
public enum TransactionKind
{
    Expense = 0,
    Income = 1,
    Transfer = 2
}
