using System.Net;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace PSAAS.Infrastructure.Tests;

public sealed class ItalianCityProviderTests
{
    private const string Json = """[{"nome":"Roma","sigla":"RM","cap":["00118","00119"]},{"nome":"Milano","sigla":"MI","cap":["20100"]},{"nome":"Romano di Lombardia","sigla":"BG","cap":["24058"]}]""";

    private sealed class Handler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(respond());
        }
    }

    // Reflection: the provider type does not exist yet.
    private static async Task<IReadOnlyList<object>?> Search(Handler h, string term, params string[] more)
    {
        var infra = typeof(PSAAS.Infrastructure.DependencyInjection).Assembly;
        var type = infra.GetType("PSAAS.Infrastructure.Geography.ItalianCityProvider");
        Assert.NotNull(type);
        var http = new HttpClient(h) { BaseAddress = new Uri("http://test.local/comuni.json") };
        var sut = Activator.CreateInstance(type!, http, new MemoryCache(new MemoryCacheOptions()))!;
        var m = type!.GetMethod("SearchAsync")!;
        // Return type is Task<IReadOnlyList<ItalianCity>?>: read the result through reflection.
        IReadOnlyList<object>? last = null;
        foreach (var t in new[] { term }.Concat(more))
        {
            var task = (Task)m.Invoke(sut, [t, CancellationToken.None])!;
            await task;
            var res = (System.Collections.IEnumerable?)task.GetType().GetProperty("Result")!.GetValue(task);
            last = res?.Cast<object>().ToList();
        }
        return last;
    }

    private static HttpResponseMessage Ok() => Ok(Json);
    private static HttpResponseMessage Ok(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json) };

    private static T Get<T>(object city, string prop) => (T)city.GetType().GetProperty(prop)!.GetValue(city)!;
    private static string[] Names(IEnumerable<object> r) => r.Select(c => Get<string>(c, "Name")).ToArray();

    [Fact]
    public async Task Search_FiltersCaseInsensitiveAndSorts()
    {
        var r = await Search(new Handler(Ok), "rOM");
        Assert.Equal(["Roma", "Romano di Lombardia"], Names(r!));
    }

    [Fact]
    public async Task Search_CallsHttpOnce_WhenRepeated()
    {
        var h = new Handler(Ok);
        await Search(h, "rom", "mil");
        Assert.Equal(1, h.Calls);
    }

    [Fact]
    public async Task Search_ReturnsNull_OnHttp500()
        => Assert.Null(await Search(new Handler(() => new HttpResponseMessage(HttpStatusCode.InternalServerError)), "rom"));

    [Fact]
    public async Task Search_ReturnsNull_OnException()
        => Assert.Null(await Search(new Handler(() => throw new HttpRequestException("boom")), "rom"));

    [Fact]
    public async Task Search_ShortTerm_ReturnsEmptyWithoutHttp()
    {
        var h = new Handler(Ok);
        var r = await Search(h, "r");
        Assert.NotNull(r);
        Assert.Empty(r!);
        Assert.Equal(0, h.Calls);
    }

    [Fact]
    public async Task Search_OrdersExactThenStartsWithThenContains()
    {
        const string json = """[{"nome":"Agromonte","sigla":"A","cap":[]},{"nome":"Romano di Lombardia","sigla":"B","cap":[]},{"nome":"Roma","sigla":"RM","cap":[]},{"nome":"Bromo","sigla":"C","cap":[]},{"nome":"Romagnano","sigla":"D","cap":[]}]""";
        var r = await Search(new Handler(() => Ok(json)), "rom");
        Assert.Equal(["Roma", "Romagnano", "Romano di Lombardia", "Agromonte", "Bromo"], Names(r!));
    }

    [Fact]
    public async Task Search_ReturnsAtMost20()
    {
        var json = "[" + string.Join(",", Enumerable.Range(0, 30).Select(i => $"{{\"nome\":\"Roma{i:00}\",\"sigla\":\"RM\",\"cap\":[]}}")) + "]";
        var r = await Search(new Handler(() => Ok(json)), "rom");
        Assert.Equal(20, r!.Count);
    }

    private sealed class GateHandler : HttpMessageHandler
    {
        public int Calls;
        public readonly TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<HttpResponseMessage> Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            Started.TrySetResult();
            return await Gate.Task.WaitAsync(ct); // honours the caller token, like the real handler
        }
    }

    // Regression: MudAutocomplete cancels the previous search on every keystroke; the download must survive.
    [Fact]
    public async Task Search_CancelledDuringDownload_DoesNotAbortDownload_AndNextSearchUsesCache()
    {
        var h = new GateHandler();
        var type = typeof(PSAAS.Infrastructure.DependencyInjection).Assembly.GetType("PSAAS.Infrastructure.Geography.ItalianCityProvider")!;
        var http = new HttpClient(h) { BaseAddress = new Uri("http://test.local/comuni.json") };
        var sut = Activator.CreateInstance(type, http, new MemoryCache(new MemoryCacheOptions()))!;
        var m = type.GetMethod("SearchAsync")!;

        using var cts = new CancellationTokenSource();
        var first = (Task)m.Invoke(sut, ["rom", cts.Token])!;
        await h.Started.Task;
        cts.Cancel();
        h.Gate.SetResult(Ok());
        try { await first; } catch (OperationCanceledException) { } // cancelled caller may get OCE or a result

        var second = (Task)m.Invoke(sut, ["rom", CancellationToken.None])!;
        await second;
        var res = ((System.Collections.IEnumerable)second.GetType().GetProperty("Result")!.GetValue(second)!).Cast<object>();
        Assert.Equal(["Roma", "Romano di Lombardia"], Names(res));
        Assert.Equal(1, h.Calls);
    }

    [Fact]
    public async Task Search_MapsProvinceCodeAndPostalCodes()
    {
        var r = await Search(new Handler(Ok), "roma");
        var roma = r!.First(c => Get<string>(c, "Name") == "Roma");
        Assert.Equal("RM", Get<string>(roma, "ProvinceCode"));
        Assert.Equal(["00118", "00119"], Get<IReadOnlyList<string>>(roma, "PostalCodes"));
    }
}
