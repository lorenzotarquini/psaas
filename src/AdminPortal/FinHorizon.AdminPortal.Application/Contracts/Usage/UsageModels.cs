namespace FinHorizon.AdminPortal.Application.Contracts.Usage;

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
