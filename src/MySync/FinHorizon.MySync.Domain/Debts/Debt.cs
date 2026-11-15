using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Debts;

/// <summary>Money the user still has to pay back (UC3). Installments are expenses linked to it.</summary>
public sealed class Debt : Entity
{
    private Debt(Guid id, OwnerId ownerId, DebtKind kind, Descriptor descriptor, Guid accountId, Money initialResidual, DateOnly createdOn)
        : base(id, ownerId)
    {
        Kind = kind;
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
    }

    public DebtKind Kind { get; private set; }

    public Descriptor Descriptor { get; private set; }

    /// <summary>Unique per user (UC3-R2); stored so the database can guard it too.</summary>
    public string NormalizedName { get; private set; }

    /// <summary>The account installments usually come from: only a default, not a constraint (D6).</summary>
    public Guid AccountId { get; private set; }

    public Money InitialResidual { get; private set; }

    public DateOnly CreatedOn { get; private set; }

    public static Debt Take(OwnerId ownerId, DebtKind kind, Descriptor descriptor, Guid accountId, Money initialResidual, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (!initialResidual.IsPositive || !Money.HasValidPrecision(initialResidual.Amount))
        {
            throw new ArgumentException("The residual must be a positive amount in cents.", nameof(initialResidual));
        }

        return new Debt(Guid.NewGuid(), ownerId, kind, descriptor, accountId, initialResidual, today);
    }

    /// <summary>What is left to pay after the given installments (UC4-R2).</summary>
    public Money ResidualAfter(Money paidInstallments) => InitialResidual - paidInstallments;
}
