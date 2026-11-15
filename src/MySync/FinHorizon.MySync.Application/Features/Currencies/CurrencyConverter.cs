using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;

namespace FinHorizon.MySync.Application.Features.Currencies;

/// <summary>The display currency of a user and the rates to convert into it (UC13).</summary>
public sealed class CurrencyConverter(IPreferencesRepository preferences, IExchangeRates exchangeRates)
{
    /// <summary>A week back finds the last rate published before a weekend or a holiday (UC13-R3).</summary>
    private const int DaysBeforePeriod = 7;

    /// <summary>The euro until the user chooses (UC13-R4).</summary>
    public async Task<string> DisplayCurrencyAsync(OwnerId owner, CancellationToken cancellationToken = default) =>
        (await preferences.GetAsync(owner, cancellationToken).ConfigureAwait(false))?.DisplayCurrency ?? CurrencyRules.Euro;

    /// <summary>
    /// The rates of <paramref name="currencies"/> from <paramref name="from"/> to <paramref name="to"/>. Amounts already in the
    /// display currency need none, so a user with one currency never depends on the rate service (UC13-5a).
    /// </summary>
    public async Task<RateTable> RatesAsync(
        string displayCurrency,
        IEnumerable<string> currencies,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var rates = new List<DailyRate>();
        foreach (var currency in currencies.Distinct().Where(currency => currency != displayCurrency))
        {
            var series = await exchangeRates.SeriesAsync(currency, displayCurrency, from.AddDays(-DaysBeforePeriod), to, cancellationToken).ConfigureAwait(false);
            if (series.Count == 0)
            {
                throw new CurrencyServiceUnavailableException($"No rate of {currency} in {displayCurrency}.");
            }

            rates.AddRange(series);
        }

        return new RateTable(displayCurrency, rates);
    }
}
