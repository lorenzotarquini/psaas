using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions;

/// <summary>An expense or an income of an account; an expense can be the installment of a debt (UC5).</summary>
public sealed class Transaction : Entity
{
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

    /// <summary>Always positive: <see cref="Kind"/> says whether it leaves or enters the account (UC5-R2).</summary>
    public Money Amount { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid CategoryId { get; private set; }

    public Guid? DebtId { get; private set; }

    public string? Note { get; private set; }

    /// <summary>When it was entered: orders movements of the same day and survives amendments (UC8-R1).</summary>
    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>Effect on the account balance: negative for an expense.</summary>
    public Money SignedAmount => Kind == TransactionKind.Expense ? -Amount : Amount;

    /// <summary>Creates a movement from data already checked by the <see cref="Rules.TransactionPolicy"/>.</summary>
    public static Transaction Record(OwnerId ownerId, TransactionData data, DateTimeOffset recordedAt)
    {
        var transaction = new Transaction(Guid.NewGuid(), ownerId, recordedAt);
        transaction.Apply(data);
        return transaction;
    }

    /// <summary>Replaces every field the user can change; identity and recording time stay (UC8-R1).</summary>
    public void Amend(TransactionData data) => Apply(data);

    private void Apply(TransactionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (!data.Amount.IsPositive || !Money.HasValidPrecision(data.Amount.Amount))
        {
            throw new ArgumentException("The amount must be positive, in cents.", nameof(data));
        }

        if (data.DebtId is not null && data.Kind != TransactionKind.Expense)
        {
            throw new ArgumentException("Only an expense can be an installment (D5).", nameof(data));
        }

        Kind = data.Kind;
        Date = data.Date;
        Amount = data.Amount;
        AccountId = data.AccountId;
        CategoryId = data.CategoryId;
        DebtId = data.DebtId;
        Note = TextNormalizer.Clean(data.Note);
    }
}
