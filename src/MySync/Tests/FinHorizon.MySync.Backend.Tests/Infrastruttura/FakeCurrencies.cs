using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Domain.Currencies;

namespace FinHorizon.MySync.Backend.Tests.Infrastruttura;

/// <summary>
/// Frankfurter replaced by fixed values (UC13 scenarios): 1 EUR = 1.10 USD, 1 BTC = 50,000 EUR.
/// Rates of single days can be set, and the service can be switched off (UC13-5a).
/// </summary>
public sealed class FakeCurrencies : ICurrencyCatalog, IExchangeRates
{
    private static readonly Dictionary<string, decimal> ValueInEuro = new()
    {
        ["EUR"] = 1m,
        ["USD"] = 1m / 1.1m,
        ["BTC"] = 50_000m,
        ["ETH"] = 3_000m,
    };

    public List<DailyRate> DayRates { get; } = [];

    public bool Unavailable { get; set; }

    public Task<IReadOnlyList<CurrencyInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        return Task.FromResult<IReadOnlyList<CurrencyInfo>>(
        [
            new("EUR", "Euro", "€", CurrencyType.Fiat),
            new("USD", "United States Dollar", "$", CurrencyType.Fiat),
            new("BTC", "Bitcoin", "₿", CurrencyType.Crypto),
            new("ETH", "Ethereum", "Ξ", CurrencyType.Crypto),
        ]);
    }

    public Task<IReadOnlyList<DailyRate>> SeriesAsync(string currency, string displayCurrency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        var days = DayRates.Where(rate => rate.Currency == currency && rate.Date >= from && rate.Date <= to).ToList();
        if (days.Count > 0 || !ValueInEuro.TryGetValue(currency, out var value) || !ValueInEuro.TryGetValue(displayCurrency, out var display))
        {
            return Task.FromResult<IReadOnlyList<DailyRate>>(days);
        }

        return Task.FromResult<IReadOnlyList<DailyRate>>([new DailyRate(currency, from, value / display)]);
    }

    private void ThrowIfUnavailable()
    {
        if (Unavailable)
        {
            throw new CurrencyServiceUnavailableException("The fake currency service is off.");
        }
    }
}
