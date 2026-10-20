namespace PSAAS.Application.Abstractions.Geography;

public sealed record ItalianCity(string Name, string ProvinceCode, IReadOnlyList<string> PostalCodes);

public interface IItalianCityProvider
{
    /// <summary>Returns matching cities; null when the city source is unavailable.</summary>
    Task<IReadOnlyList<ItalianCity>?> SearchAsync(string term, CancellationToken ct);
}
