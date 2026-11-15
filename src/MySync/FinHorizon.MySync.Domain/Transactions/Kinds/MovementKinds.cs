using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions.Kinds;

/// <summary>Money leaves one account (UC5).</summary>
public sealed class ExpenseKind : IMovementKind
{
    public TransactionKind Kind => TransactionKind.Expense;

    public bool UsesCategory => true;

    public bool UsesCounterAccount => false;

    public bool CanPayDebt => true;

    public IReadOnlyList<Posting> Postings(TransactionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return [new Posting(data.AccountId, -data.Amount)];
    }
}

/// <summary>Money enters one account (UC5).</summary>
public sealed class IncomeKind : IMovementKind
{
    public TransactionKind Kind => TransactionKind.Income;

    public bool UsesCategory => true;

    public bool UsesCounterAccount => false;

    public bool CanPayDebt => false;

    public IReadOnlyList<Posting> Postings(TransactionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return [new Posting(data.AccountId, data.Amount)];
    }
}

/// <summary>
/// Money moves from one account of the user to another (UC9): neither an expense nor an income,
/// so the net worth does not change.
/// </summary>
public sealed class TransferKind : IMovementKind
{
    public TransactionKind Kind => TransactionKind.Transfer;

    public bool UsesCategory => false;

    public bool UsesCounterAccount => true;

    public bool CanPayDebt => false;

    public IReadOnlyList<Posting> Postings(TransactionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var destination = data.CounterAccountId ?? throw new ArgumentException("A transfer needs a destination account.", nameof(data));
        return [new Posting(data.AccountId, -data.Amount), new Posting(destination, data.Amount)];
    }
}

/// <summary>The registry of movement kinds.</summary>
public static class MovementKinds
{
    private static readonly IReadOnlyDictionary<TransactionKind, IMovementKind> All =
        new IMovementKind[] { new ExpenseKind(), new IncomeKind(), new TransferKind() }.ToDictionary(kind => kind.Kind);

    public static IMovementKind Of(TransactionKind kind) =>
        All.TryGetValue(kind, out var found) ? found : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown movement kind.");

    /// <summary>Kinds that carry a category: the only valid kinds of a category (UC6).</summary>
    public static IReadOnlyList<TransactionKind> WithCategory { get; } = All.Values.Where(kind => kind.UsesCategory).Select(kind => kind.Kind).ToList();

    /// <summary>Net effect on the user's money: zero for a transfer.</summary>
    public static Money NetChange(IEnumerable<Posting> postings) => Money.Sum(postings.Select(posting => posting.Amount));
}
