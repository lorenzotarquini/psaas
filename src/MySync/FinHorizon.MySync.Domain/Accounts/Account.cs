using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Accounts;

/// <summary>A place where the user keeps money or value (UC2). Its balance changes only through movements.</summary>
public sealed class Account : Entity
{
    private Account(Guid id, OwnerId ownerId, AccountKind kind, Descriptor descriptor, Money initialBalance, DateOnly openedOn)
        : base(id, ownerId)
    {
        Kind = kind;
        Descriptor = descriptor;
        NormalizedName = descriptor.NormalizedName;
        InitialBalance = initialBalance;
        OpenedOn = openedOn;
    }

    /// <summary>For materialization by the persistence layer only.</summary>
    private Account()
    {
        Descriptor = null!;
        NormalizedName = null!;
    }

    public AccountKind Kind { get; private set; }

    public Descriptor Descriptor { get; private set; }

    /// <summary>Unique per user (UC2-R2); stored so the database can guard it too.</summary>
    public string NormalizedName { get; private set; }

    /// <summary>The balance on <see cref="OpenedOn"/>; it can be zero or negative (D7).</summary>
    public Money InitialBalance { get; private set; }

    /// <summary>The day the user created the account in MySync; no movement can precede it (D3).</summary>
    public DateOnly OpenedOn { get; private set; }

    public static Account Open(OwnerId ownerId, AccountKind kind, Descriptor descriptor, Money initialBalance, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (!Money.HasValidPrecision(initialBalance.Amount))
        {
            throw new ArgumentException("The initial balance has more than 2 decimals.", nameof(initialBalance));
        }

        return new Account(Guid.NewGuid(), ownerId, kind, descriptor, initialBalance, today);
    }

    /// <summary>Balance after the signed movements of this account (UC4-R1).</summary>
    public Money BalanceAfter(Money signedMovements) => InitialBalance + signedMovements;
}
