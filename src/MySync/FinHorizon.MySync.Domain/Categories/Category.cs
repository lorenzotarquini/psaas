using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;
using FinHorizon.MySync.Domain.Transactions.Kinds;

namespace FinHorizon.MySync.Domain.Categories;

/// <summary>A label of the user for expenses or for income (UC6). Expense and income categories are separate.</summary>
public sealed class Category : Entity
{
    public const int MaxNameLength = 40;

    private Category(Guid id, OwnerId ownerId, TransactionKind kind, string name)
        : base(id, ownerId)
    {
        Kind = kind;
        Name = name;
        NormalizedName = TextNormalizer.Normalize(name);
    }

    /// <summary>For materialization by the persistence layer only.</summary>
    private Category()
    {
        Name = null!;
        NormalizedName = null!;
    }

    public TransactionKind Kind { get; private set; }

    public string Name { get; private set; }

    /// <summary>Unique per user and kind (UC6-R2).</summary>
    public string NormalizedName { get; private set; }

    /// <summary>UC15: a fixed expense (rent, bills) rather than a variable one. Every category is born variable (UC15-R1).</summary>
    public bool Fixed { get; private set; }

    public static Category Create(OwnerId ownerId, TransactionKind kind, string name)
    {
        var cleaned = TextNormalizer.Clean(name) ?? throw new ArgumentException("A name is required.", nameof(name));
        if (cleaned.Length > MaxNameLength)
        {
            throw new ArgumentException("The name is too long.", nameof(name));
        }

        if (!MovementKinds.WithCategory.Contains(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        return new Category(Guid.NewGuid(), ownerId, kind, cleaned);
    }

    /// <summary>UC15: only expenses are fixed or variable; an income category is never fixed.</summary>
    public void SetFixed(bool isFixed)
    {
        if (isFixed && Kind != TransactionKind.Expense)
        {
            throw new InvalidOperationException("Only an expense category can be fixed.");
        }

        Fixed = isFixed;
    }
}
