using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;

namespace FinHorizon.MySync.Domain.Debts;

/// <summary>Money the user still has to pay back (UC3). Installments are expenses linked to it.</summary>
public sealed class Debt : Entity
{
    private Debt(Guid id, OwnerId ownerId, DebtKind kind, Descriptor descriptor, Guid accountId, string currency, Money initialResidual, DateOnly createdOn)
        : base(id, ownerId)
    {
        Kind = kind;
        Currency = currency;
        Descriptor = descriptor;
        NormalizedName = descriptor.NormalizedName;
        AccountId = accountId;
        InitialResidual = initialResidual;
        CreatedOn = createdOn;
    }

    /// <summary>For materialization by the persistence layer only.</summary>
    private Debt()
    {
        Descriptor = null!;
        NormalizedName = null!;
        Currency = null!;
    }

    public DebtKind Kind { get; private set; }

    public Descriptor Descriptor { get; private set; }

    /// <summary>Unique per user (UC3-R2); stored so the database can guard it too.</summary>
    public string NormalizedName { get; private set; }

    /// <summary>The account installments usually come from: only a default, not a constraint (D6).</summary>
    public Guid AccountId { get; private set; }

    /// <summary>A traditional currency; installments come from accounts in the same currency (UC13-R8).</summary>
    public string Currency { get; private set; }

    public Money InitialResidual { get; private set; }

    public DateOnly CreatedOn { get; private set; }

    public static Debt Take(OwnerId ownerId, DebtKind kind, Descriptor descriptor, Guid accountId, string currency, Money initialResidual, DateOnly today)
    {
        Check(kind, descriptor, currency);
        if (!initialResidual.IsPositive || !Money.HasValidPrecision(initialResidual.Amount))
        {
            throw new ArgumentException("The residual must be a positive amount in cents.", nameof(initialResidual));
        }

        return new Debt(Guid.NewGuid(), ownerId, kind, descriptor, accountId, currency, initialResidual, today);
    }

    /// <summary>
    /// UC10: everything but the creation day can change. The new initial residual cannot be below what was already paid,
    /// or the residual would turn negative (UC10-R4); the currency changes only while nothing was paid (UC13-R6).
    /// </summary>
    public void Update(DebtKind kind, Descriptor descriptor, Guid accountId, string currency, Money initialResidual, Money paidInstallments)
    {
        Check(kind, descriptor, currency);
        if (currency != Currency && paidInstallments != Money.Zero)
        {
            throw new InvalidOperationException("The currency of a debt with installments cannot change.");
        }

        if (!initialResidual.IsPositive || !Money.HasValidPrecision(initialResidual.Amount) || initialResidual < paidInstallments)
        {
            throw new ArgumentException("The residual must be a positive amount in cents, not below what was already paid.", nameof(initialResidual));
        }

        Kind = kind;
        Descriptor = descriptor;
        NormalizedName = descriptor.NormalizedName;
        AccountId = accountId;
        Currency = currency;
        InitialResidual = initialResidual;
    }

    /// <summary>What is left to pay after the given installments (UC4-R2).</summary>
    public Money ResidualAfter(Money paidInstallments) => InitialResidual - paidInstallments;

    private static void Check(DebtKind kind, Descriptor descriptor, string currency)
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
    }
}
