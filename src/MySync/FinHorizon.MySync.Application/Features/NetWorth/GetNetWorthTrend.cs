using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Currencies;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.NetWorth;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.NetWorth;

/// <summary>UC7: the trend over "30d", "90d", "12m" or "all"; 30 days when not given.</summary>
/// <summary>UC7; the switches of UC12 decide whether stable assets and investments are in.</summary>
public sealed record GetNetWorthTrendQuery(string? Period, bool IncludeStable = true, bool IncludeInvestments = true);

public sealed class GetNetWorthTrendValidator : AbstractValidator<GetNetWorthTrendQuery>
{
    public GetNetWorthTrendValidator() =>
        RuleFor(query => query.Period)
            .Must(period => period is null || NetWorthPeriod.TryParse(period, out _))
            .WithErrorCode(ErrorCodes.InvalidValue);
}

public sealed record NetWorthPointView(DateOnly Date, decimal NetWorth, decimal Debts);

/// <summary>The trend in <see cref="Currency"/>, the display currency, converted day by day (UC13-R3).</summary>
public sealed record NetWorthTrendView(string Period, IReadOnlyList<NetWorthPointView> Points, decimal Change, decimal? ChangePercent, string Currency);

public sealed class GetNetWorthTrendHandler(ICurrentUser user, ILedgerReader ledger, ICalendar calendar, CurrencyConverter converter)
    : IRequestHandler<GetNetWorthTrendQuery, NetWorthTrendView>
{
    public async Task<Result<NetWorthTrendView>> HandleAsync(GetNetWorthTrendQuery request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        // The validator already refused unknown codes: here only "not given" falls back to 30 days.
        if (!NetWorthPeriod.TryParse(request.Period, out var period))
        {
            period = NetWorthPeriod.Days30;
        }

        var history = await ledger.HistoryAsync(user.Id, PortfolioScope.For(request.IncludeStable, request.IncludeInvestments), cancellationToken).ConfigureAwait(false);
        var today = calendar.Today;
        var display = await converter.DisplayCurrencyAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var from = history.Start is { } start ? period.StartFor(today, start) : today;
        var rates = await converter.RatesAsync(display, history.Currencies, from, today, cancellationToken).ConfigureAwait(false);
        var trend = NetWorthCalculator.Trend(history, period, today, rates);
        return Result<NetWorthTrendView>.Ok(new NetWorthTrendView(
            period.Code,
            trend.Points.Select(point => new NetWorthPointView(point.Date, point.NetWorth.Amount, point.Debts.Amount)).ToList(),
            trend.Change.Amount,
            trend.ChangePercent,
            display));
    }
}
