using System.Net;
using System.Text;
using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Infrastructure.Currencies;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace FinHorizon.MySync.Infrastructure.Tests;

/// <summary>UC13-R1, R9: the answers of Frankfurter v2 as they really look (checked on 2026-11-15).</summary>
public sealed class FrankfurterCurrenciesTests : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public void Dispose() => _cache.Dispose();

    [Fact]
    public async Task Catalog_KeepsCurrenciesAndCryptos_NotMetals()
    {
        var service = Service(_ => Json("""
            [{"iso_code":"BTC","iso_numeric":null,"name":"Bitcoin","symbol":"₿","type":"crypto"},
             {"iso_code":"EUR","iso_numeric":"978","name":"Euro","symbol":"€","type":"currency"},
             {"iso_code":"XAU","iso_numeric":"959","name":"Gold","symbol":null,"type":"metal"}]
            """));

        var catalog = await service.ListAsync();

        Assert.Equal(
            [new CurrencyInfo("BTC", "Bitcoin", "₿", CurrencyType.Crypto), new CurrencyInfo("EUR", "Euro", "€", CurrencyType.Fiat)],
            catalog);
    }

    [Fact]
    public async Task Rates_AreAskedOnceAnHourPerPeriod()
    {
        var calls = new List<string>();
        var service = Service(request =>
        {
            calls.Add(request.RequestUri!.PathAndQuery);
            return Json("""[{"date":"2026-11-14","base":"BTC","quote":"EUR","rate":62503},{"date":"2026-11-15","base":"BTC","quote":"EUR","rate":62523}]""");
        });
        var from = new DateOnly(2026, 11, 14);
        var to = new DateOnly(2026, 11, 15);

        var rates = await service.SeriesAsync("BTC", "EUR", from, to);
        await service.SeriesAsync("BTC", "EUR", from, to);

        Assert.Equal([new DailyRate("BTC", from, 62503m), new DailyRate("BTC", to, 62523m)], rates);
        Assert.Equal(["/v2/rates?base=BTC&quotes=EUR&from=2026-11-14&to=2026-11-15"], calls);
    }

    [Fact]
    public async Task AnErrorOfTheService_IsUnavailable()
    {
        var service = Service(_ => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity));

        await Assert.ThrowsAsync<CurrencyServiceUnavailableException>(() => service.SeriesAsync("XYZ", "EUR", new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2)));
    }

    private FrankfurterCurrencies Service(Func<HttpRequestMessage, HttpResponseMessage> answer) => new(new StubClients(answer), _cache);

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubClients(Func<HttpRequestMessage, HttpResponseMessage> answer) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(answer)) { BaseAddress = new Uri("https://frankfurter.test/") };
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(answer(request));
    }
}
