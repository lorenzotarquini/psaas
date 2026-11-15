namespace FinHorizon.MySync.Domain.NetWorth;

/// <summary>Which days of a period become points of the chart (UC7-R3). The last day is always kept.</summary>
public interface ISeriesSampling
{
    bool Keeps(DateOnly day, DateOnly lastDay);
}

/// <summary>One point per day.</summary>
public sealed class DailySampling : ISeriesSampling
{
    public bool Keeps(DateOnly day, DateOnly lastDay) => true;
}

/// <summary>One point every seven days, counted back from the last day.</summary>
public sealed class WeeklySampling : ISeriesSampling
{
    public bool Keeps(DateOnly day, DateOnly lastDay) => (lastDay.DayNumber - day.DayNumber) % 7 == 0;
}

/// <summary>The last day of every month, and the last day of the period.</summary>
public sealed class MonthlySampling : ISeriesSampling
{
    public bool Keeps(DateOnly day, DateOnly lastDay) => day == lastDay || day.AddDays(1).Day == 1;
}
