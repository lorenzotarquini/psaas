using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.NetWorth;

/// <summary>The points of a period and how much the net worth changed over it (UC7 step 5).</summary>
public sealed record NetWorthTrend(IReadOnlyList<NetWorthPoint> Points, Money Change, decimal? ChangePercent)
{
    public static NetWorthTrend Empty { get; } = new([], Money.Zero, null);

    public static NetWorthTrend Of(IReadOnlyList<NetWorthPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            return Empty;
        }

        var first = points[0].NetWorth;
        var change = points[^1].NetWorth - first;
        // UC7-5a: no percentage of nothing.
        decimal? percent = first.Amount == 0
            ? null
            : decimal.Round(change.Amount / Math.Abs(first.Amount) * 100m, 1, MidpointRounding.AwayFromZero);
        return new NetWorthTrend(points, change, percent);
    }
}
