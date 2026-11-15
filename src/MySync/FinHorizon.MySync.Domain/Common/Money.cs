using System.Globalization;

namespace FinHorizon.MySync.Domain.Common;

/// <summary>An amount in euro (RNF3). Always decimal, never floating point: the single place to add currencies later.</summary>
public readonly record struct Money(decimal Amount) : IComparable<Money>
{
    public static readonly Money Zero = new(0m);

    public bool IsPositive => Amount > 0;

    public bool IsNegative => Amount < 0;

    /// <summary>Euro cents are the smallest unit: 10.005 is not a valid amount.</summary>
    public static bool HasValidPrecision(decimal amount) => decimal.Round(amount, 2) == amount;

    public static Money Sum(IEnumerable<Money> values) => new(values.Sum(value => value.Amount));

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

    public static Money operator -(Money value) => new(-value.Amount);

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    public override string ToString() => Amount.ToString("0.00", CultureInfo.InvariantCulture);
}
