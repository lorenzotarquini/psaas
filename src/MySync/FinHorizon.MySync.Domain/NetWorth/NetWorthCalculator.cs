using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.NetWorth;

/// <summary>
/// Turns the ledger history into points (UC7-R1, R2): every value counts from its day on,
/// an installment lowers both the net worth (it is an expense) and the debts.
/// </summary>
public static class NetWorthCalculator
{
    public static NetWorthPoint At(LedgerHistory history, DateOnly day)
    {
        ArgumentNullException.ThrowIfNull(history);
        var netWorth = Sum(history.Accounts, day) + Money.Sum(history.Movements.Where(m => m.Date <= day).Select(m => m.BalanceChange));
        var debts = Sum(history.Debts, day) - Money.Sum(history.Movements.Where(m => m.Date <= day).Select(m => m.InstallmentsPaid));
        return new NetWorthPoint(day, netWorth, debts);
    }

    public static NetWorthTrend Trend(LedgerHistory history, NetWorthPeriod period, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(period);
        if (history.Start is not { } firstAccount || firstAccount > today)
        {
            return NetWorthTrend.Empty;
        }

        var start = period.StartFor(today, firstAccount);
        var points = new List<NetWorthPoint>();
        var current = At(history, start.AddDays(-1));
        var movements = history.Movements.ToLookup(movement => movement.Date);
        for (var day = start; day <= today; day = day.AddDays(1))
        {
            current = Advance(current, day, history, movements[day]);
            if (period.Sampling.Keeps(day, today))
            {
                points.Add(current);
            }
        }

        return NetWorthTrend.Of(points);
    }

    private static NetWorthPoint Advance(NetWorthPoint previous, DateOnly day, LedgerHistory history, IEnumerable<DailyMovement> movements)
    {
        var netWorth = previous.NetWorth + OpenedOn(history.Accounts, day);
        var debts = previous.Debts + OpenedOn(history.Debts, day);
        foreach (var movement in movements)
        {
            netWorth += movement.BalanceChange;
            debts -= movement.InstallmentsPaid;
        }

        return new NetWorthPoint(day, netWorth, debts);
    }

    private static Money Sum(IEnumerable<OpeningValue> values, DateOnly day) =>
        Money.Sum(values.Where(value => value.From <= day).Select(value => value.Amount));

    private static Money OpenedOn(IEnumerable<OpeningValue> values, DateOnly day) =>
        Money.Sum(values.Where(value => value.From == day).Select(value => value.Amount));
}
