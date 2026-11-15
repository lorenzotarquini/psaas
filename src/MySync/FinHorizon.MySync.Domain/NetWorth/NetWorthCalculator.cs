using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;

namespace FinHorizon.MySync.Domain.NetWorth;

/// <summary>
/// Turns the ledger history into points (UC7-R1, R2): every value counts from its day on,
/// an installment lowers both the net worth (it is an expense) and the debts.
/// Balances are kept per currency and converted with the rate of each day (UC13-R3).
/// </summary>
public static class NetWorthCalculator
{
    public static NetWorthPoint At(LedgerHistory history, DateOnly day, RateTable? rates = null)
    {
        ArgumentNullException.ThrowIfNull(history);
        var balances = new Balances();
        balances.AddUntil(history, day);
        return balances.PointOn(day, rates ?? RateTable.Euro);
    }

    public static NetWorthTrend Trend(LedgerHistory history, NetWorthPeriod period, DateOnly today, RateTable? rates = null)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(period);
        if (history.Start is not { } firstAccount || firstAccount > today)
        {
            return NetWorthTrend.Empty;
        }

        var table = rates ?? RateTable.Euro;
        var start = period.StartFor(today, firstAccount);
        var balances = new Balances();
        balances.AddUntil(history, start.AddDays(-1));
        var accounts = history.Accounts.ToLookup(value => value.From);
        var debts = history.Debts.ToLookup(value => value.From);
        var movements = history.Movements.ToLookup(movement => movement.Date);
        var points = new List<NetWorthPoint>();
        for (var day = start; day <= today; day = day.AddDays(1))
        {
            balances.Add(accounts[day], debts[day], movements[day]);
            if (period.Sampling.Keeps(day, today))
            {
                points.Add(balances.PointOn(day, table));
            }
        }

        return NetWorthTrend.Of(points);
    }

    /// <summary>The running net worth and debts, one sum per currency.</summary>
    private sealed class Balances
    {
        private readonly Dictionary<string, decimal> _netWorth = [];
        private readonly Dictionary<string, decimal> _debts = [];

        public void AddUntil(LedgerHistory history, DateOnly day) =>
            Add(
                history.Accounts.Where(value => value.From <= day),
                history.Debts.Where(value => value.From <= day),
                history.Movements.Where(movement => movement.Date <= day));

        public void Add(IEnumerable<OpeningValue> accounts, IEnumerable<OpeningValue> debts, IEnumerable<DailyMovement> movements)
        {
            foreach (var account in accounts)
            {
                Increase(_netWorth, account.Currency, account.Amount.Amount);
            }

            foreach (var debt in debts)
            {
                Increase(_debts, debt.Currency, debt.Amount.Amount);
            }

            foreach (var movement in movements)
            {
                Increase(_netWorth, movement.Currency, movement.BalanceChange.Amount);
                Increase(_debts, movement.Currency, -movement.InstallmentsPaid.Amount);
            }
        }

        public NetWorthPoint PointOn(DateOnly day, RateTable rates) => new(day, Convert(_netWorth, day, rates), Convert(_debts, day, rates));

        private static Money Convert(Dictionary<string, decimal> sums, DateOnly day, RateTable rates) =>
            new(RateTable.Round(sums.Sum(sum => rates.ToDisplay(sum.Key, sum.Value, day))));

        private static void Increase(Dictionary<string, decimal> sums, string currency, decimal amount) =>
            sums[currency] = sums.GetValueOrDefault(currency) + amount;
    }
}
