namespace FinHorizon.Modules.Sdk;

/// <summary>How the values of a metric combine within a day (UC25-R1).</summary>
public static class UsageKinds
{
    /// <summary>Values of the same day add up (e.g. documents created).</summary>
    public const string Counter = "counter";

    /// <summary>The last value of the day wins (e.g. storage used).</summary>
    public const string Gauge = "gauge";
}

/// <param name="At">When it was measured; now when missing.</param>
public sealed record UsageRecordRequest(Guid? TenantId, string? ModuleId, string? Metric, decimal? Value, string? Kind, DateTimeOffset? At);

/// <summary>What a module POSTs to /api/usage (role UsageReporter), up to 500 records (UC25).</summary>
public sealed record UsageReport(IReadOnlyList<UsageRecordRequest>? Records);
