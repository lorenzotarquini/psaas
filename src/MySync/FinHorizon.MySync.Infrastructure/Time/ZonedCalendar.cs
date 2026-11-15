using FinHorizon.MySync.Domain.Common;
using Microsoft.Extensions.Options;

namespace FinHorizon.MySync.Infrastructure.Time;

/// <summary>Section "Calendar": the time zone that decides what "today" is for the users.</summary>
public sealed class CalendarOptions
{
    public const string SectionName = "Calendar";

    public string TimeZone { get; set; } = "Europe/Rome";
}

/// <summary>Today in the configured time zone, from the system clock (replaceable in tests).</summary>
internal sealed class ZonedCalendar(TimeProvider clock, IOptions<CalendarOptions> options) : ICalendar
{
    private readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), _zone).DateTime);
}
