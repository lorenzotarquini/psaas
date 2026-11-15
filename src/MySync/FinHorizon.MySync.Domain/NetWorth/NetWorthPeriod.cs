namespace FinHorizon.MySync.Domain.NetWorth;

/// <summary>
/// The periods of the trend (UC7 step 2). Each one knows where it starts and how it is sampled,
/// so a new period is one more instance here and nothing else.
/// </summary>
public sealed class NetWorthPeriod
{
    public static readonly NetWorthPeriod Days30 = new("30d", today => today.AddDays(-29), new DailySampling());
    public static readonly NetWorthPeriod Days90 = new("90d", today => today.AddDays(-89), new DailySampling());
    public static readonly NetWorthPeriod Months12 = new("12m", today => today.AddMonths(-12).AddDays(1), new WeeklySampling());
    public static readonly NetWorthPeriod All = new("all", _ => DateOnly.MinValue, new MonthlySampling());

    private static readonly IReadOnlyDictionary<string, NetWorthPeriod> ByCode =
        new[] { Days30, Days90, Months12, All }.ToDictionary(period => period.Code, StringComparer.OrdinalIgnoreCase);

    private readonly Func<DateOnly, DateOnly> _startFrom;

    private NetWorthPeriod(string code, Func<DateOnly, DateOnly> startFrom, ISeriesSampling sampling)
    {
        Code = code;
        _startFrom = startFrom;
        Sampling = sampling;
    }

    public string Code { get; }

    public ISeriesSampling Sampling { get; }

    public static IReadOnlyCollection<string> Codes => ByCode.Keys.ToList();

    public static bool TryParse(string? code, out NetWorthPeriod period)
    {
        period = Days30;
        if (code is null || !ByCode.TryGetValue(code, out var found))
        {
            return false;
        }

        period = found;
        return true;
    }

    /// <summary>The first day of the period, never before the first account (UC7-2a).</summary>
    public DateOnly StartFor(DateOnly today, DateOnly firstAccountOpenedOn)
    {
        var start = _startFrom(today);
        return start > firstAccountOpenedOn ? start : firstAccountOpenedOn;
    }

    public override string ToString() => Code;
}
