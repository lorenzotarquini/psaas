using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NLog;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Audit;

public sealed class AuditLog(PsaasDbContext dbContext, ICurrentActor actor, TimeProvider clock) : IAuditLog, IAuditReadRepository
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task RecordAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        var entry = new AuditEventDbModel
        {
            Id = Guid.NewGuid(),
            OccurredAt = clock.GetUtcNow(),
            ActorId = actor.Id,
            ActorName = actor.Name,
            Action = record.Action,
            TargetType = record.TargetType,
            TargetId = record.TargetId,
            TargetName = record.TargetName,
            TenantId = record.TenantId,
            Details = record.Details is null ? null : JsonSerializer.Serialize(record.Details, Json),
        };

        try
        {
            dbContext.Set<AuditEventDbModel>().Add(entry);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            Logger.Debug("Audit {Action} on {TargetType} {TargetId} by {ActorId}", entry.Action, entry.TargetType, entry.TargetId, entry.ActorId);
        }
        catch (Exception exception) when (exception is DbUpdateException or InvalidOperationException)
        {
            // The change already happened: losing its audit line must not turn it into an error.
            dbContext.Entry(entry).State = EntityState.Detached;
            Logger.Error(exception, "Audit {Action} on {TargetType} {TargetId} by {ActorId} not recorded", entry.Action, entry.TargetType, entry.TargetId, entry.ActorId);
        }
    }

    public async Task<PagedResult<AuditEventReadModel>> GetPageAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var events = dbContext.Set<AuditEventDbModel>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.TenantId))
        {
            events = events.Where(e => e.TenantId == query.TenantId);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            events = events.Where(e => e.Action.StartsWith(query.Action));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            events = events.Where(e => e.ActorName.ToLower().Contains(term) || e.TargetName.ToLower().Contains(term));
        }

        if (query.From is { } from)
        {
            events = events.Where(e => e.OccurredAt >= from);
        }

        if (query.To is { } to)
        {
            events = events.Where(e => e.OccurredAt < to);
        }

        var total = await events.CountAsync(cancellationToken).ConfigureAwait(false);
        var page = await events
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<AuditEventReadModel>(
            page.Select(e => new AuditEventReadModel(
                e.Id,
                e.OccurredAt,
                e.ActorId,
                e.ActorName,
                e.Action,
                e.TargetType,
                e.TargetId,
                e.TargetName,
                e.TenantId,
                e.Details is null ? null : JsonDocument.Parse(e.Details).RootElement.Clone())).ToArray(),
            total,
            query.Page,
            query.PageSize);
    }
}
