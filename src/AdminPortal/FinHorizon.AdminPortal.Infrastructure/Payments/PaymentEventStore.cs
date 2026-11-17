using Microsoft.EntityFrameworkCore;
using Npgsql;
using FinHorizon.AdminPortal.Application.Abstractions.Payments;
using FinHorizon.AdminPortal.Application.Contracts.Payments;
using FinHorizon.AdminPortal.Infrastructure.Data.DbContexts;

namespace FinHorizon.AdminPortal.Infrastructure.Payments;

/// <summary>One row per Stripe event (UC33); the id of Stripe is the key, so a duplicate cannot slip in.</summary>
public sealed class PaymentEventDbModel
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}

public sealed class PaymentEventStore(AdminPortalDbContext dbContext) : IPaymentEventStore
{
    private const string UniqueViolationSqlState = "23505";

    public async Task<bool> TryAddAsync(ReceivedPaymentEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentEvent);
        if (await dbContext.PaymentEvents.AnyAsync(e => e.Id == paymentEvent.Id, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        var row = new PaymentEventDbModel
        {
            Id = paymentEvent.Id,
            Type = paymentEvent.Type,
            CreatedAt = paymentEvent.CreatedAt,
            ReceivedAt = paymentEvent.ReceivedAt,
        };
        dbContext.PaymentEvents.Add(row);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            // The same event delivered twice at once: the other request stored it (UC33-R2).
            dbContext.Entry(row).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<IReadOnlyList<ReceivedPaymentEvent>> GetLatestAsync(int take, CancellationToken cancellationToken = default) =>
        await dbContext.PaymentEvents.AsNoTracking()
            .OrderByDescending(e => e.ReceivedAt)
            .Take(take)
            .Select(e => new ReceivedPaymentEvent(e.Id, e.Type, e.CreatedAt, e.ReceivedAt))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
}
