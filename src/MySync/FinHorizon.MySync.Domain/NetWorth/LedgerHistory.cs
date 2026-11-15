using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;

namespace FinHorizon.MySync.Domain.NetWorth;

/// <summary>A starting value that counts from a day on: the initial balance of an account or the initial residual of a debt, in its currency.</summary>
public sealed record OpeningValue(DateOnly From, Money Amount, string Currency = CurrencyRules.Euro);

/// <summary>
/// The movements of one day in one currency, summed: <see cref="BalanceChange"/> is incomes minus expenses on every account,
/// <see cref="InstallmentsPaid"/> the part of the expenses that paid debts back.
/// </summary>
public sealed record DailyMovement(DateOnly Date, Money BalanceChange, Money InstallmentsPaid, string Currency = CurrencyRules.Euro);

/// <summary>Everything the net worth of a user depends on (UC7-R1, R2), already reduced to sums.</summary>
public sealed record LedgerHistory(
    IReadOnlyList<OpeningValue> Accounts,
    IReadOnlyList<OpeningValue> Debts,
    IReadOnlyList<DailyMovement> Movements)
{
    public static LedgerHistory Empty { get; } = new([], [], []);

    /// <summary>The day the first account was opened, when there is one (UC7-2a).</summary>
    public DateOnly? Start => Accounts.Count == 0 ? null : Accounts.Min(account => account.From);

    /// <summary>Every currency the history is in: the rates the trend needs (UC13).</summary>
    public IReadOnlySet<string> Currencies =>
        Accounts.Select(value => value.Currency).Concat(Debts.Select(value => value.Currency)).Concat(Movements.Select(movement => movement.Currency)).ToHashSet();
}
