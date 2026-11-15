using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions.Kinds;

namespace FinHorizon.MySync.Domain.Transactions;

/// <summary>
/// A movement of money: an expense, an income (UC5) or a transfer between two accounts (UC9).
/// Its effect on the balances is its <see cref="Postings"/>, decided by its <see cref="IMovementKind"/>.
/// </summary>
public sealed class Transaction : Entity
{
    private readonly List<Posting> _postings = [];

    private Transaction(Guid id, OwnerId ownerId, DateTimeOffset recordedAt)
        : base(id, ownerId)
    {
        RecordedAt = recordedAt;
    }

    /// <summary>For materialization by the persistence layer only.</summary>
    private Transaction()
    {
    }

    public TransactionKind Kind { get; private set; }

    public DateOnly Date { get; private set; }

    /// <summary>Always positive: the postings say where it leaves and where it enters (UC5-R2).</summary>
    public Money Amount { get; private set; }

    /// <summary>The account of an expense or income; the source account of a transfer.</summary>
    public Guid AccountId { get; private set; }

    /// <summary>The destination account of a transfer.</summary>
    public Guid? CounterAccountId { get; private set; }

    /// <summary>What reaches the destination of a transfer between two currencies (UC13-R7); otherwise null.</summary>
    public Money? CounterAmount { get; private set; }

    public Guid? CategoryId { get; private set; }

    public Guid? DebtId { get; private set; }

    public string? Note { get; private set; }

    /// <summary>When it was entered: orders movements of the same day and survives amendments (UC8-R1).</summary>
    public DateTimeOffset RecordedAt { get; private set; }

    public IReadOnlyList<Posting> Postings => _postings;

    /// <summary>Creates a movement from data already checked by the <see cref="Rules.TransactionPolicy"/>.</summary>
    public static Transaction Record(OwnerId ownerId, TransactionData data, DateTimeOffset recordedAt)
    {
        var transaction = new Transaction(Guid.NewGuid(), ownerId, recordedAt);
        transaction.Apply(data);
        return transaction;
    }

    /// <summary>Replaces every field the user can change, postings included; identity and recording time stay (UC8-R1).</summary>
    public void Amend(TransactionData data) => Apply(data);

    private void Apply(TransactionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var kind = MovementKinds.Of(data.Kind);
        // The decimals of each currency are a rule (AmountsMustFitCurrenciesRule): here only the widest precision.
        if (!IsValidAmount(data.Amount) || (data.CounterAmount is { } counter && !IsValidAmount(counter)))
        {
            throw new ArgumentException("The amounts must be positive, with at most 8 decimals.", nameof(data));
        }

        if (data.CounterAmount is not null && !kind.UsesCounterAccount)
        {
            throw new ArgumentException($"A {data.Kind} has no counter amount.", nameof(data));
        }

        if ((data.DebtId is not null && !kind.CanPayDebt)
            || (data.CategoryId is not null) != kind.UsesCategory
            || (data.CounterAccountId is not null) != kind.UsesCounterAccount
            || data.CounterAccountId == data.AccountId)
        {
            throw new ArgumentException($"The fields do not match a {data.Kind}.", nameof(data));
        }

        Kind = data.Kind;
        Date = data.Date;
        Amount = data.Amount;
        AccountId = data.AccountId;
        CounterAccountId = data.CounterAccountId;
        CounterAmount = data.CounterAmount;
        CategoryId = data.CategoryId;
        DebtId = data.DebtId;
        Note = TextNormalizer.Clean(data.Note);
        _postings.Clear();
        _postings.AddRange(kind.Postings(data));
    }

    private static bool IsValidAmount(Money amount) => amount.IsPositive && Money.HasValidPrecision(amount.Amount, CurrencyRules.MaxDecimals);
}
