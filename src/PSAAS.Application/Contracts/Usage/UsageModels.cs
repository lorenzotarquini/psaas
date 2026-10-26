namespace PSAAS.Application.Contracts.Usage;

/// <summary>UC25-R1.</summary>
public static class UsageKinds
{
    /// <summary>Values of the same day add up (e.g. documents created).</summary>
    public const string Counter = "counter";

    /// <summary>The last value of the day wins (e.g. storage used).</summary>
    public const string Gauge = "gauge";
}

/// <param name="At">When it was measured; now when missing.</param>
public sealed record UsageRecordRequest(Guid? TenantId, string? ModuleId, string? Metric, decimal? Value, string? Kind, DateTimeOffset? At);

/// <summary>UC25 step 1: what a module sends, up to 500 records.</summary>
public sealed record UsageReport(IReadOnlyList<UsageRecordRequest>? Records);

/// <summary>A validated record, at day granularity.</summary>
public sealed record UsageRecord(Guid TenantId, string ModuleId, string Metric, string Kind, DateOnly Day, decimal Value);

public sealed record UsagePoint(DateOnly Day, decimal Value);

/// <param name="Total">Sum over the period for a counter, last value for a gauge (UC25-R3).</param>
public sealed record UsageSeries(string ModuleId, string ModuleName, string Metric, string Kind, decimal Total, IReadOnlyList<UsagePoint> Points);

/// <summary>UC25 step 2: the "Uso" tab. Users and active users are null when Keycloak could not count them.</summary>
public sealed record TenantUsage(
    int Days,
    int? Users,
    int? MaxUsers,
    int? ActiveUsers,
    int ProductiveUnits,
    int? MaxProductiveUnits,
    IReadOnlyList<UsageSeries> Metrics);
