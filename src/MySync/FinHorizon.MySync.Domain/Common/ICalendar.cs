namespace FinHorizon.MySync.Domain.Common;

/// <summary>The user's calendar: movements carry dates, not instants (RNF4).</summary>
public interface ICalendar
{
    DateOnly Today { get; }
}
