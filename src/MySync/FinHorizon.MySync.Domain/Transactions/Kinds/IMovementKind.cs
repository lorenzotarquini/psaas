namespace FinHorizon.MySync.Domain.Transactions.Kinds;

/// <summary>
/// What a kind of movement is: which fields it uses and how it moves money between accounts.
/// A new kind is one more implementation, registered in <see cref="MovementKinds"/>; balances, lists and the trend follow its postings.
/// </summary>
public interface IMovementKind
{
    TransactionKind Kind { get; }

    /// <summary>Expenses and incomes carry a category of the same kind; transfers carry none.</summary>
    bool UsesCategory { get; }

    /// <summary>A transfer moves money to a second account of the user.</summary>
    bool UsesCounterAccount { get; }

    /// <summary>Only an expense can be the installment of a debt (D5).</summary>
    bool CanPayDebt { get; }

    /// <summary>How the movement changes the balances of the accounts it touches.</summary>
    IReadOnlyList<Posting> Postings(TransactionData data);
}
