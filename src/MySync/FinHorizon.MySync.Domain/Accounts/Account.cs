using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;

namespace FinHorizon.MySync.Domain.Accounts;

/// <summary>A place where the user keeps money or value (UC2). Its balance changes only through movements.</summary>
public sealed class Account : Entity
{
    private Account(Guid id, OwnerId ownerId, AccountKind kind, Descriptor descriptor, string currency, Money initialBalance, DateOnly openedOn)
        : base(id, ownerId)
    {
        Kind = kind;
        Currency = currency;
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
        Currency = null!;
    }

    public AccountKind Kind { get; private set; }

    public Descriptor Descriptor { get; private set; }

    /// <summary>The code of the currency every amount of the account is in: a cryptocurrency only for <see cref="AccountKind.VirtualCurrency"/> (UC13).</summary>
    public string Currency { get; private set; }

    public CurrencyType CurrencyType => CurrencyRules.TypeFor(Kind);

    /// <summary>Unique per user (UC2-R2); stored so the database can guard it too.</summary>
    public string NormalizedName { get; private set; }

    /// <summary>The balance on <see cref="OpenedOn"/>; it can be zero or negative (D7).</summary>
    public Money InitialBalance { get; private set; }

    /// <summary>The day the user created the account in MySync; no movement can precede it (D3).</summary>
    public DateOnly OpenedOn { get; private set; }

    public static Account Open(OwnerId ownerId, AccountKind kind, Descriptor descriptor, string currency, Money initialBalance, DateOnly today)
    {
        Check(kind, descriptor, currency, initialBalance);
        return new Account(Guid.NewGuid(), ownerId, kind, descriptor, currency, initialBalance, today);
    }

    /// <summary>
    /// UC10: kind, name, description, tag, currency and initial balance can change; the opening day cannot (movements depend on it).
    /// A new initial balance changes every balance and the whole net worth trend, which are computed from it.
    /// The currency changes only while no movement is recorded in it (UC13-R6).
    /// </summary>
    public void Update(AccountKind kind, Descriptor descriptor, string currency, Money initialBalance, bool hasMovements)
    {
        Check(kind, descriptor, currency, initialBalance);
        if (hasMovements && !KeepsCurrency(kind, currency))
        {
            throw new InvalidOperationException("The currency of an account with movements cannot change.");
        }

        Kind = kind;
        Currency = currency;
        Descriptor = descriptor;
        NormalizedName = descriptor.NormalizedName;
        InitialBalance = initialBalance;
    }

    /// <summary>Balance after the signed movements of this account (UC4-R1).</summary>
    public Money BalanceAfter(Money signedMovements) => InitialBalance + signedMovements;

    /// <summary>Whether an amount fits the decimals of the account currency (UC13-R2).</summary>
    public bool Fits(Money amount) => CurrencyRules.HasValidPrecision(amount.Amount, CurrencyType);

    /// <summary>Same currency, so the amounts already recorded keep their meaning (UC13-R6).</summary>
    public bool KeepsCurrency(AccountKind kind, string currency) => currency == Currency && CurrencyRules.TypeFor(kind) == CurrencyType;

    private static void Check(AccountKind kind, Descriptor descriptor, string currency, Money initialBalance)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (!CurrencyRules.IsWellFormed(currency))
        {
            throw new ArgumentException("Not a currency code.", nameof(currency));
        }

        if (!CurrencyRules.HasValidPrecision(initialBalance.Amount, CurrencyRules.TypeFor(kind)))
        {
            throw new ArgumentException("The initial balance has more decimals than its currency.", nameof(initialBalance));
        }
    }
}
