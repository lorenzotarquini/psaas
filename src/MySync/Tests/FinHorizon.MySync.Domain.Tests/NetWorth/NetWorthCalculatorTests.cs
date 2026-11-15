using FinHorizon.MySync.Domain.NetWorth;
using Xunit;
using static FinHorizon.MySync.Domain.Tests.Builders;

namespace FinHorizon.MySync.Domain.Tests.NetWorth;

/// <summary>The arithmetic behind UC4 and UC7.</summary>
public sealed class NetWorthCalculatorTests
{
    private static readonly DateOnly Nov12 = new(2026, 11, 12);

    /// <summary>UC7-S1: account on the 12th, debt on the 13th, installment on the 14th.</summary>
    private static readonly LedgerHistory Uc7S1 = new(
        [new OpeningValue(Nov12, Euro(1000m))],
        [new OpeningValue(Nov12.AddDays(1), Euro(500m))],
        [new DailyMovement(Nov12.AddDays(2), Euro(-100m), Euro(100m))]);

    [Fact]
    public void Trend_FollowsUc7S1()
    {
        var trend = NetWorthCalculator.Trend(Uc7S1, NetWorthPeriod.Days30, Today);

        Assert.Equal(
            [
                (Nov12, 1000m, 0m),
                (Nov12.AddDays(1), 1000m, 500m),
                (Nov12.AddDays(2), 900m, 400m),
                (Today, 900m, 400m)
            ],
            trend.Points.Select(p => (p.Date, p.NetWorth.Amount, p.Debts.Amount)));
        Assert.Equal(Euro(-100m), trend.Change);
        Assert.Equal(-10.0m, trend.ChangePercent);
    }

    [Fact]
    public void At_CountsOnlyWhatExistedThatDay()
    {
        Assert.Equal(Euro(0m), NetWorthCalculator.At(Uc7S1, Nov12.AddDays(-1)).NetWorth);
        Assert.Equal((Euro(900m), Euro(400m)), (NetWorthCalculator.At(Uc7S1, Today).NetWorth, NetWorthCalculator.At(Uc7S1, Today).Debts));
    }

    /// <summary>The debts are shown next to the net worth, never subtracted from it.</summary>
    [Fact]
    public void Debts_DoNotLowerTheNetWorth()
    {
        var history = new LedgerHistory([new OpeningValue(Today, Euro(6023m))], [new OpeningValue(Today, Euro(170m))], []);

        Assert.Equal(Euro(6023m), NetWorthCalculator.At(history, Today).NetWorth);
    }

    [Fact]
    public void Trend_WithoutAccountsIsEmpty() =>
        Assert.Empty(NetWorthCalculator.Trend(LedgerHistory.Empty, NetWorthPeriod.All, Today).Points);

    [Fact]
    public void Trend_StartsAtThePeriodWhenTheFirstAccountIsOlder()
    {
        var history = new LedgerHistory([new OpeningValue(Today.AddYears(-2), Euro(10_000m))], [], []);

        var trend = NetWorthCalculator.Trend(history, NetWorthPeriod.Days90, Today);

        Assert.Equal(90, trend.Points.Count);
        Assert.Equal(Today.AddDays(-89), trend.Points[0].Date);
        Assert.Equal(Euro(10_000m), trend.Points[0].NetWorth);
    }

    [Fact]
    public void Trend_ZeroStartHasNoPercentage()
    {
        var history = new LedgerHistory([new OpeningValue(Today.AddDays(-1), Euro(0m))], [], [new DailyMovement(Today, Euro(50m), Euro(0m))]);

        var trend = NetWorthCalculator.Trend(history, NetWorthPeriod.Days30, Today);

        Assert.Equal(Euro(50m), trend.Change);
        Assert.Null(trend.ChangePercent);
    }

    /// <summary>UC7-S3.</summary>
    [Fact]
    public void Change_IsAmountAndPercentage()
    {
        var trend = NetWorthTrend.Of([new NetWorthPoint(Today.AddDays(-29), Euro(10_000m), Euro(0m)), new NetWorthPoint(Today, Euro(10_500m), Euro(0m))]);

        Assert.Equal(Euro(500m), trend.Change);
        Assert.Equal(5.0m, trend.ChangePercent);
    }

    [Fact]
    public void LongPeriods_AreSampledAndEndToday()
    {
        var history = new LedgerHistory([new OpeningValue(Today.AddYears(-3), Euro(1m))], [], []);

        var year = NetWorthCalculator.Trend(history, NetWorthPeriod.Months12, Today);
        var all = NetWorthCalculator.Trend(history, NetWorthPeriod.All, Today);

        Assert.All(year.Points.Zip(year.Points.Skip(1)), pair => Assert.Equal(7, pair.Second.Date.DayNumber - pair.First.Date.DayNumber));
        Assert.Equal(Today, year.Points[^1].Date);
        Assert.Equal(Today, all.Points[^1].Date);
        Assert.All(all.Points.SkipLast(1), point => Assert.Equal(1, point.Date.AddDays(1).Day));
    }

    [Theory]
    [InlineData("30d", true)]
    [InlineData("12M", true)]
    [InlineData("all", true)]
    [InlineData("7y", false)]
    [InlineData(null, false)]
    public void Period_IsParsedFromItsCode(string? code, bool known) => Assert.Equal(known, NetWorthPeriod.TryParse(code, out _));
}
