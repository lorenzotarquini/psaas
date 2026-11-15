using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Domain.Currencies;
using Microsoft.Extensions.Caching.Memory;
using NLog;

namespace FinHorizon.MySync.Infrastructure.Currencies;

/// <summary>Where the currency service is: AppHost parameter mysync-rates-url (UC13-R1).</summary>
public sealed class CurrencyServiceOptions
{
    public const string SectionName = "Currencies";

    [Required]
    public Uri? Url { get; set; }
}

/// <summary>
/// Catalog and daily rates from Frankfurter v2 (D10): <c>GET v2/currencies</c> and
/// <c>GET v2/rates?base=&amp;quotes=&amp;from=&amp;to=</c>. Kept in memory: the catalog a day, the rates an hour (UC13-R9).
/// </summary>
internal sealed class FrankfurterCurrencies(IHttpClientFactory httpClients, IMemoryCache cache) : ICurrencyCatalog, IExchangeRates
{
    public const string ClientName = "frankfurter";

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly TimeSpan CatalogLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan RatesLifetime = TimeSpan.FromHours(1);

    public async Task<IReadOnlyList<CurrencyInfo>> ListAsync(CancellationToken cancellationToken = default) =>
        await GetCachedAsync("currencies", CatalogLifetime, async () =>
        {
            var rows = await GetAsync<List<CurrencyRow>>("v2/currencies", cancellationToken).ConfigureAwait(false);
            IReadOnlyList<CurrencyInfo> catalog = rows
                .Select(row => (Row: row, Type: TypeOf(row.Type)))
                .Where(entry => entry.Type is not null && CurrencyRules.IsWellFormed(entry.Row.IsoCode))
                .Select(entry => new CurrencyInfo(entry.Row.IsoCode, entry.Row.Name, entry.Row.Symbol, entry.Type!.Value))
                .ToList();
            return catalog;
        }).ConfigureAwait(false);

    public async Task<IReadOnlyList<DailyRate>> SeriesAsync(string currency, string displayCurrency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await GetCachedAsync($"rates:{currency}:{displayCurrency}:{from:O}:{to:O}", RatesLifetime, async () =>
        {
            var url = string.Create(
                CultureInfo.InvariantCulture,
                $"v2/rates?base={Uri.EscapeDataString(currency)}&quotes={Uri.EscapeDataString(displayCurrency)}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
            var rows = await GetAsync<List<RateRow>>(url, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<DailyRate> rates = rows
                .Where(row => row.Quote == displayCurrency && row.Rate > 0)
                .Select(row => new DailyRate(currency, row.Date, row.Rate))
                .ToList();
            return rates;
        }).ConfigureAwait(false);

    /// <summary>Metals (XAU, …) are not currencies an account can hold.</summary>
    private static CurrencyType? TypeOf(string type) => type switch
    {
        "currency" => CurrencyType.Fiat,
        "crypto" => CurrencyType.Crypto,
        _ => null
    };

    private async Task<T> GetCachedAsync<T>(string key, TimeSpan lifetime, Func<Task<T>> load)
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await load().ConfigureAwait(false);
        cache.Set(key, value, lifetime);
        return value;
    }

    private async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClients.CreateClient(ClientName);
            return await client.GetFromJsonAsync<T>(url, cancellationToken).ConfigureAwait(false)
                ?? throw new CurrencyServiceUnavailableException("Empty answer from the currency service.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            Logger.Warn(exception, "Currency service unavailable for {Path}", url.Split('?')[0]);
            throw new CurrencyServiceUnavailableException("The currency service does not answer.", exception);
        }
    }

    private sealed record CurrencyRow(
        [property: JsonPropertyName("iso_code")] string IsoCode,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("symbol")] string? Symbol,
        [property: JsonPropertyName("type")] string Type);

    private sealed record RateRow(
        [property: JsonPropertyName("date")] DateOnly Date,
        [property: JsonPropertyName("quote")] string Quote,
        [property: JsonPropertyName("rate")] decimal Rate);
}
