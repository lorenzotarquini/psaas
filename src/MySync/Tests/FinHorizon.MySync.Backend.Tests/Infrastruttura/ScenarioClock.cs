namespace FinHorizon.MySync.Backend.Tests.Infrastruttura;

/// <summary>
/// The backend clock of a scenario: "today" can be placed anywhere, also in the past, to build a history (UC7).
/// Every reading moves it 1 ms forward so movements entered one after the other keep their order.
/// </summary>
public sealed class ScenarioClock : TimeProvider
{
    private readonly Lock _gate = new();
    private DateTimeOffset _now = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            _now = _now.AddMilliseconds(1);
            return _now;
        }
    }

    public void Set(DateTimeOffset now)
    {
        lock (_gate)
        {
            _now = now;
        }
    }
}
