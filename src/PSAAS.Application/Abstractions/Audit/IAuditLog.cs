using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Audit;

namespace PSAAS.Application.Abstractions.Audit;

/// <summary>Who is acting: the administrator of the request, or the system for scheduled jobs.</summary>
public interface ICurrentActor
{
    string Id { get; }

    string Name { get; }
}

/// <summary>
/// EXP-10: append-only record of who did what, when, on which tenant. Written after the change succeeded;
/// a failure to record is logged and never undoes or fails the change itself.
/// </summary>
public interface IAuditLog
{
    Task RecordAsync(AuditRecord record, CancellationToken cancellationToken = default);
}

public interface IAuditReadRepository
{
    /// <summary>Newest first.</summary>
    Task<PagedResult<AuditEventReadModel>> GetPageAsync(AuditQuery query, CancellationToken cancellationToken = default);
}
