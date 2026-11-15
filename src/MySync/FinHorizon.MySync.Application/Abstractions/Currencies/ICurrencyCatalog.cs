using FinHorizon.MySync.Domain.Currencies;

namespace FinHorizon.MySync.Application.Abstractions.Currencies;

/// <summary>A currency the user can choose (UC13): ISO code or crypto ticker, name and symbol.</summary>
public sealed record CurrencyInfo(string Code, string Name, string? Symbol, CurrencyType Type);

/// <summary>The currencies of UC13, traditional and crypto; an external service behind it (D10).</summary>
public interface ICurrencyCatalog
{
    /// <exception cref="CurrencyServiceUnavailableException">The service does not answer.</exception>
    Task<IReadOnlyList<CurrencyInfo>> ListAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The value of currencies in the display currency, day by day (UC13-R3). Only days with a published rate are returned:
/// weekends and holidays are missing and the <see cref="RateTable"/> carries the last rate over them.
/// </summary>
public interface IExchangeRates
{
    /// <exception cref="CurrencyServiceUnavailableException">The service does not answer.</exception>
    Task<IReadOnlyList<DailyRate>> SeriesAsync(string currency, string displayCurrency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}

/// <summary>UC13-5a: the catalog or the rates cannot be read now; the API answers 503.</summary>
public sealed class CurrencyServiceUnavailableException : Exception
{
    public CurrencyServiceUnavailableException()
    {
    }

    public CurrencyServiceUnavailableException(string message)
        : base(message)
    {
    }

    public CurrencyServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public static class CurrencyCatalogExtensions
{
    /// <summary>Whether the code is in the catalog with that type: a crypto ticker is not a valid currency for a current account.</summary>
    public static async Task<bool> HasAsync(this ICurrencyCatalog catalog, string code, CurrencyType type, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var currencies = await catalog.ListAsync(cancellationToken).ConfigureAwait(false);
        return currencies.Any(currency => currency.Code == code && currency.Type == type);
    }
}
