using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.NetWorth;

/// <summary>A starting value that counts from a day on: the initial balance of an account or the initial residual of a debt.</summary>
public sealed record OpeningValue(DateOnly From, Money Amount);

/// <summary>
/// The movements of one day, summed: <see cref="BalanceChange"/> is incomes minus expenses on every account,
/// <see cref="InstallmentsPaid"/> the part of the expenses that paid debts back.
/// </summary>
public sealed record DailyMovement(DateOnly Date, Money BalanceChange, Money InstallmentsPaid);

/// <summary>Everything the net worth of a user depends on (UC7-R1, R2), already reduced to sums.</summary>
public sealed record LedgerHistory(
    IReadOnlyList<OpeningValue> Accounts,
    IReadOnlyList<OpeningValue> Debts,
    IReadOnlyList<DailyMovement> Movements)
{
    public static LedgerHistory Empty { get; } = new([], [], []);

    /// <summary>The day the first account was opened, when there is one (UC7-2a).</summary>
    public DateOnly? Start => Accounts.Count == 0 ? null : Accounts.Min(account => account.From);
}
