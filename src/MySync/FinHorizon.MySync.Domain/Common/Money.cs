using System.Globalization;

namespace FinHorizon.MySync.Domain.Common;

/// <summary>
/// An amount in the currency of its account or debt (RNF3, UC13). Always decimal, never floating point.
/// Trailing zeros are dropped, so 1000.00000000 read from the database is 1000.
/// </summary>
public readonly record struct Money(decimal Amount) : IComparable<Money>
{
    public static readonly Money Zero = new(0m);

    // Dividing by 1.000…0 keeps the value and removes the scale: the documented way to normalize a decimal.
    public decimal Amount { get; } = Amount / 1.000000000000000000000000000000000m;

    public bool IsPositive => Amount > 0;

    public bool IsNegative => Amount < 0;

    /// <summary>Euro cents by default: 10.005 is not a valid amount in euro; a crypto amount can have more decimals (UC13-R2).</summary>
    public static bool HasValidPrecision(decimal amount, int decimals = 2) => decimal.Round(amount, decimals) == amount;

    public static Money Sum(IEnumerable<Money> values) => new(values.Sum(value => value.Amount));

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

    public static Money operator -(Money value) => new(-value.Amount);

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    public override string ToString() => Amount.ToString("0.00######", CultureInfo.InvariantCulture);
}
