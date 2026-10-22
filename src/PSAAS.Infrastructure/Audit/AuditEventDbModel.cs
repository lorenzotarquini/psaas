namespace PSAAS.Infrastructure.Audit;

public sealed class AuditEventDbModel
{
    public Guid Id { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string ActorId { get; set; } = string.Empty;

    public string ActorName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public string? TenantId { get; set; }

    /// <summary>jsonb: action specific data.</summary>
    public string? Details { get; set; }
}
