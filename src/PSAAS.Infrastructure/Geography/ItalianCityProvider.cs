using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PSAAS.Application.Abstractions.Geography;

namespace PSAAS.Infrastructure.Geography;

public sealed class ItalianCityProvider(
    HttpClient http,
    IMemoryCache cache,
    ILogger<ItalianCityProvider> logger) : IItalianCityProvider
{
    private const string CacheKey = "italian-cities";

    public async Task<IReadOnlyList<ItalianCity>?> SearchAsync(string term, CancellationToken ct)
    {
        if (term is null || term.Trim().Length < 2) return [];

        try
        {
            if (!cache.TryGetValue(CacheKey, out ItalianCity[]? cities) || cities is null)
            {
                Task<ItalianCity[]> download;
                lock (_lock) download = _download ??= DownloadAsync();
                // Search cancelled (MudAutocomplete, every keystroke): return empty without throwing.
                if (await Task.WhenAny(download, Task.Delay(Timeout.Infinite, ct)) != download) return [];
                cities = await download;
            }

            var t = term.Trim();
            return cities
                .Where(c => c.Name.Contains(t, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Name.Equals(t, StringComparison.OrdinalIgnoreCase) ? 0 : c.Name.StartsWith(t, StringComparison.OrdinalIgnoreCase) ? 1 : 2)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or OperationCanceledException)
        {
            if (ct.IsCancellationRequested) throw;
            logger.LogWarning(ex, "Italian city list unavailable");
            return null;
        }
    }

    private readonly object _lock = new();
    private Task<ItalianCity[]>? _download;

    // Shared download, independent from the search token (MudAutocomplete cancels at every keystroke).
    private async Task<ItalianCity[]> DownloadAsync()
    {
        try
        {
            var items = await http.GetFromJsonAsync<List<CityDto>>("", CancellationToken.None);
            var cities = (items ?? [])
                .Where(c => !string.IsNullOrWhiteSpace(c.Nome))
                .Select(c => new ItalianCity(c.Nome!, c.Sigla ?? "", c.Cap ?? []))
                .ToArray();
            cache.Set(CacheKey, cities, TimeSpan.FromHours(24));
            return cities;
        }
        finally
        {
            lock (_lock) _download = null;
        }
    }

    private sealed record CityDto(string? Nome, string? Sigla, List<string>? Cap);
}
