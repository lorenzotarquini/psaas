using Microsoft.EntityFrameworkCore;
using Npgsql;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Domain;
using PSAAS.Infrastructure.Converters;
using PSAAS.Infrastructure.Data.DbContexts;
using PSAAS.Infrastructure.Data.DbModels;

namespace PSAAS.Infrastructure.Repositories.Tenants;

/// <summary>
/// Write-side repository persisting tenants through PsaasDbContext.
/// </summary>
public sealed class TenantWriteRepository(PsaasDbContext dbContext) : ITenantWriteRepository
{
    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        var dbModel = TenantDbModelConverter.ToDbModel(tenant);
        dbContext.Tenants.Add(dbModel);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DetachFailedInsert(dbModel);
            throw;
        }
        catch (DbUpdateException exception)
        {
            DetachFailedInsert(dbModel);

            if (IsUniqueViolation(exception))
            {
                throw new TenantUniquenessConflictException(
                    "A tenant with the same company name, complete VAT number or certified email already exists.",
                    exception);
            }

            throw;
        }
    }

    // The DbContext is scoped to the Blazor circuit, so a failed insert must not
    // stay tracked: a stale 'Added' entry would be re-sent by any later retry
    // from the same user session.
    private void DetachFailedInsert(TenantDbModel dbModel)
    {
        dbContext.Entry(dbModel).State = EntityState.Detached;
    }

    // PostgreSQL SQLSTATE for unique_violation.
    private const string UniqueViolationSqlState = "23505";

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: UniqueViolationSqlState
        };
}
