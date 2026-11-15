using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Application.Common;

namespace FinHorizon.MySync.Application.Features.Currencies;

/// <summary>UC13: the catalog the user chooses the currency of an account, a debt or the display from.</summary>
public sealed record ListCurrenciesQuery;

public sealed class ListCurrenciesHandler(ICurrencyCatalog catalog) : IRequestHandler<ListCurrenciesQuery, IReadOnlyList<CurrencyInfo>>
{
    public async Task<Result<IReadOnlyList<CurrencyInfo>>> HandleAsync(ListCurrenciesQuery request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CurrencyInfo> currencies = (await catalog.ListAsync(cancellationToken).ConfigureAwait(false))
            .OrderBy(currency => currency.Type)
            .ThenBy(currency => currency.Code, StringComparer.Ordinal)
            .ToList();
        return Result<IReadOnlyList<CurrencyInfo>>.Ok(currencies);
    }
}
