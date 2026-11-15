namespace FinHorizon.MySync.Domain.Currencies;

/// <summary>The value of one unit of <see cref="Currency"/> in the display currency, on <see cref="Date"/>.</summary>
public sealed record DailyRate(string Currency, DateOnly Date, decimal Rate);

/// <summary>
/// Converts amounts into the display currency with the rate of their day (UC13-R3): the last rate published on or
/// before that day, or the first one known when the day comes before every rate.
/// </summary>
public sealed class RateTable
{
    private readonly Dictionary<string, (List<DateOnly> Dates, List<decimal> Rates)> _byCurrency;

    public RateTable(string displayCurrency, IEnumerable<DailyRate> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);
        DisplayCurrency = displayCurrency;
        _byCurrency = rates
            .GroupBy(rate => rate.Currency)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var ordered = group.OrderBy(rate => rate.Date).ToList();
                    return (ordered.Select(rate => rate.Date).ToList(), ordered.Select(rate => rate.Rate).ToList());
                });
    }

    public string DisplayCurrency { get; }

    /// <summary>For a user whose amounts are all in euro: nothing to convert.</summary>
    public static RateTable Euro { get; } = new(CurrencyRules.Euro, []);

    public bool Knows(string currency) => currency == DisplayCurrency || _byCurrency.ContainsKey(currency);

    public decimal ToDisplay(string currency, decimal amount, DateOnly day)
    {
        if (currency == DisplayCurrency || amount == 0)
        {
            return amount;
        }

        if (!_byCurrency.TryGetValue(currency, out var series) || series.Dates.Count == 0)
        {
            throw new InvalidOperationException($"No rate of {currency} in {DisplayCurrency}.");
        }

        var index = series.Dates.BinarySearch(day);
        // Not found: ~index is the first later day, so the one before it is the last earlier rate.
        var at = index >= 0 ? index : Math.Max(~index - 1, 0);
        return amount * series.Rates[at];
    }

    /// <summary>Converted amounts are shown in cents (UC13-R2).</summary>
    public static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
