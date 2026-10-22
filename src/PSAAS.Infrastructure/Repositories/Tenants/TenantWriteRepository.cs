using Microsoft.EntityFrameworkCore;
using NLog;
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
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        Logger.Debug("Persisting tenant {TenantId}", tenant.Id);

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
                Logger.Warn("Tenant unique violation while saving tenant {TenantId}", tenant.Id);
                throw new TenantUniquenessConflictException(
                    "A tenant with the same company name, complete VAT number or certified email already exists.",
                    exception);
            }

            throw;
        }
    }

    public async Task AddWithProductiveUnitsAsync(
        Tenant tenant,
        IReadOnlyCollection<TenantProductiveUnitCreateModel> productiveUnits,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        Logger.Debug("Persisting tenant {TenantId}", tenant.Id);
        ArgumentNullException.ThrowIfNull(productiveUnits);

        var tenantDbModel = TenantDbModelConverter.ToDbModel(tenant);
        var productiveUnitDbModels = productiveUnits
            .Select(productiveUnit => new ProductiveUnitDbModel
            {
                Id = Guid.NewGuid(),
                Name = productiveUnit.Name,
                Description = productiveUnit.Description,
                TenantId = tenant.Id.ToString()
            })
            .ToArray();
        var productiveUnitProfileDbModels = BuildProductiveUnitProfiles(
            productiveUnits,
            productiveUnitDbModels,
            new Dictionary<string, string>(StringComparer.Ordinal));

        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : null;

        dbContext.Tenants.Add(tenantDbModel);
        dbContext.ProductiveUnits.AddRange(productiveUnitDbModels);
        dbContext.ProductiveUnitProfiles.AddRange(productiveUnitProfileDbModels);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DetachFailedInsert(tenantDbModel);
            DetachTracked(productiveUnitDbModels);
            DetachTracked(productiveUnitProfileDbModels);
            throw;
        }
        catch (DbUpdateException exception)
        {
            DetachFailedInsert(tenantDbModel);
            DetachTracked(productiveUnitDbModels);
            DetachTracked(productiveUnitProfileDbModels);

            if (IsProductiveUnitNameUniqueViolation(exception))
            {
                Logger.Warn("Productive unit name unique violation while saving tenant {TenantId}", tenant.Id);
                throw new ProductiveUnitNameConflictException(
                    "A productive unit with the same name already exists.",
                    exception);
            }

            if (IsUniqueViolation(exception))
            {
                Logger.Warn("Tenant unique violation while saving tenant {TenantId}", tenant.Id);
                throw new TenantUniquenessConflictException(
                    "A tenant with the same company name, complete VAT number or certified email already exists.",
                    exception);
            }

            throw;
        }
    }

    public async Task AddWithProfilesAndProductiveUnitsAsync(
        Tenant tenant,
        IReadOnlyCollection<TenantProfileCreateModel> profiles,
        IReadOnlyCollection<TenantProductiveUnitCreateModel> productiveUnits,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        Logger.Debug("Persisting tenant {TenantId}", tenant.Id);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(productiveUnits);

        var tenantDbModel = TenantDbModelConverter.ToDbModel(tenant);
        var profileIdMap = profiles.ToDictionary(
            profile => profile.TemporaryId,
            _ => Guid.NewGuid().ToString(),
            StringComparer.Ordinal);
        var profileDbModels = profiles
            .Select(profile => new ProfileDbModel
            {
                Id = profileIdMap[profile.TemporaryId],
                TenantId = tenant.Id.ToString(),
                Name = profile.Name,
                Description = profile.Description
            })
            .ToArray();
        var productiveUnitDbModels = productiveUnits
            .Select(productiveUnit => new ProductiveUnitDbModel
            {
                Id = Guid.NewGuid(),
                Name = productiveUnit.Name,
                Description = productiveUnit.Description,
                TenantId = tenant.Id.ToString()
            })
            .ToArray();
        var productiveUnitProfileDbModels = BuildProductiveUnitProfiles(
            productiveUnits,
            productiveUnitDbModels,
            profileIdMap);

        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : null;

        dbContext.Tenants.Add(tenantDbModel);
        dbContext.Profiles.AddRange(profileDbModels);
        dbContext.ProductiveUnits.AddRange(productiveUnitDbModels);
        dbContext.ProductiveUnitProfiles.AddRange(productiveUnitProfileDbModels);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DetachFailedInsert(tenantDbModel);
            DetachTracked(profileDbModels);
            DetachTracked(productiveUnitDbModels);
            DetachTracked(productiveUnitProfileDbModels);
            throw;
        }
        catch (DbUpdateException exception)
        {
            DetachFailedInsert(tenantDbModel);
            DetachTracked(profileDbModels);
            DetachTracked(productiveUnitDbModels);
            DetachTracked(productiveUnitProfileDbModels);

            if (IsProductiveUnitNameUniqueViolation(exception))
            {
                Logger.Warn("Productive unit name unique violation while saving tenant {TenantId}", tenant.Id);
                throw new ProductiveUnitNameConflictException(
                    "A productive unit with the same name already exists.",
                    exception);
            }

            if (IsUniqueViolation(exception))
            {
                Logger.Warn("Tenant unique violation while saving tenant {TenantId}", tenant.Id);
                throw new TenantUniquenessConflictException(
                    "A tenant, profile or productive unit with the same unique value already exists.",
                    exception);
            }

            throw;
        }
    }

    public async Task<bool> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        Logger.Debug("Persisting tenant {TenantId}", tenant.Id);

        var existing = await dbContext.Tenants
            .SingleOrDefaultAsync(
                dbTenant => dbTenant.Id == tenant.Id.ToString(),
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            Logger.Warn("Tenant {TenantId} not found for update", tenant.Id);
            return false;
        }

        var updated = TenantDbModelConverter.ToDbModel(tenant);
        existing.CompanyName = updated.CompanyName;
        existing.NormalizedCompanyName = updated.NormalizedCompanyName;
        existing.VatNumber = updated.VatNumber;
        existing.CompleteVatNumber = updated.CompleteVatNumber;
        existing.CertifiedEmail = updated.CertifiedEmail;
        existing.NormalizedCertifiedEmail = updated.NormalizedCertifiedEmail;
        existing.Address = updated.Address;
        existing.SdiCode = updated.SdiCode;
        existing.AccountManager = updated.AccountManager;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DetachTracked(existing);
            throw;
        }
        catch (DbUpdateException exception)
        {
            DetachTracked(existing);

            if (IsUniqueViolation(exception))
            {
                Logger.Warn("Tenant unique violation while saving tenant {TenantId}", tenant.Id);
                throw new TenantUniquenessConflictException(
                    "A tenant with the same company name, complete VAT number or certified email already exists.",
                    exception);
            }

            throw;
        }
    }

    public async Task<bool> UpdateWithStructureAsync(
        Tenant tenant,
        IReadOnlyCollection<TenantProfileEditModel> profiles,
        IReadOnlyCollection<TenantProductiveUnitEditModel> productiveUnits,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(productiveUnits);
        Logger.Debug("Persisting tenant {TenantId} with structure", tenant.Id);

        var tenantId = tenant.Id.ToString();
        var existing = await dbContext.Tenants
            .SingleOrDefaultAsync(dbTenant => dbTenant.Id == tenantId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            Logger.Warn("Tenant {TenantId} not found for update", tenant.Id);
            return false;
        }

        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : null;

        var updated = TenantDbModelConverter.ToDbModel(tenant);
        existing.CompanyName = updated.CompanyName;
        existing.NormalizedCompanyName = updated.NormalizedCompanyName;
        existing.VatNumber = updated.VatNumber;
        existing.CompleteVatNumber = updated.CompleteVatNumber;
        existing.CertifiedEmail = updated.CertifiedEmail;
        existing.NormalizedCertifiedEmail = updated.NormalizedCertifiedEmail;
        existing.Address = updated.Address;
        existing.SdiCode = updated.SdiCode;
        existing.AccountManager = updated.AccountManager;

        try
        {
            // Always filtered by TenantId: other tenants' rows are never loaded.
            var dbProfiles = await dbContext.Profiles
                .Where(profile => profile.TenantId == tenantId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var dbUnits = await dbContext.ProductiveUnits
                .Include(unit => unit.ProductiveUnitProfiles)
                .Where(unit => unit.TenantId == tenantId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var profileIdMap = new Dictionary<Guid, string>();
            var keptProfiles = new HashSet<string>(StringComparer.Ordinal);
            foreach (var profile in profiles)
            {
                var dbProfile = profile.Id is null
                    ? null
                    : dbProfiles.SingleOrDefault(p => p.Id == profile.Id)
                        ?? throw new InvalidOperationException($"Profile {profile.Id} does not belong to tenant {tenantId}.");
                if (dbProfile is null)
                {
                    dbProfile = new ProfileDbModel { Id = Guid.NewGuid().ToString(), TenantId = tenantId };
                    dbContext.Profiles.Add(dbProfile);
                }

                dbProfile.Name = profile.Name;
                dbProfile.Description = profile.Description;
                profileIdMap[profile.ClientId] = dbProfile.Id;
                keptProfiles.Add(dbProfile.Id);
            }

            var keptUnits = new HashSet<Guid>();
            foreach (var unit in productiveUnits)
            {
                var dbUnit = unit.Id is null
                    ? null
                    : dbUnits.SingleOrDefault(u => u.Id == unit.Id)
                        ?? throw new InvalidOperationException($"Productive unit {unit.Id} does not belong to tenant {tenantId}.");
                if (dbUnit is null)
                {
                    dbUnit = new ProductiveUnitDbModel { Id = Guid.NewGuid(), TenantId = tenantId };
                    dbContext.ProductiveUnits.Add(dbUnit);
                }

                dbUnit.Name = unit.Name;
                dbUnit.Description = unit.Description;
                keptUnits.Add(dbUnit.Id);

                var desired = unit.ProfileClientIds.Select(id => profileIdMap[id]).ToHashSet(StringComparer.Ordinal);
                foreach (var link in dbUnit.ProductiveUnitProfiles.Where(l => !desired.Contains(l.ProfileId)).ToList())
                {
                    dbContext.ProductiveUnitProfiles.Remove(link);
                }

                foreach (var profileId in desired.Except(dbUnit.ProductiveUnitProfiles.Select(l => l.ProfileId)).ToList())
                {
                    dbContext.ProductiveUnitProfiles.Add(new ProductiveUnitProfileDbModel
                    {
                        ProductiveUnitId = dbUnit.Id,
                        ProfileId = profileId
                    });
                }
            }

            foreach (var removedUnit in dbUnits.Where(u => !keptUnits.Contains(u.Id)))
            {
                dbContext.ProductiveUnitProfiles.RemoveRange(removedUnit.ProductiveUnitProfiles);
                dbContext.ProductiveUnits.Remove(removedUnit);
            }

            dbContext.Profiles.RemoveRange(dbProfiles.Where(p => !keptProfiles.Contains(p.Id)));

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dbContext.ChangeTracker.Clear();
            throw;
        }
        catch (Exception exception)
        {
            dbContext.ChangeTracker.Clear();

            if (exception is DbUpdateException dbUpdate && IsProductiveUnitNameUniqueViolation(dbUpdate))
            {
                Logger.Warn("Productive unit name unique violation while updating tenant {TenantId}", tenant.Id);
                throw new ProductiveUnitNameConflictException("A productive unit with the same name already exists.", dbUpdate);
            }

            if (exception is DbUpdateException unique && IsUniqueViolation(unique))
            {
                Logger.Warn("Unique violation while updating tenant {TenantId}", tenant.Id);
                throw new TenantUniquenessConflictException(
                    "A tenant, profile or productive unit with the same unique value already exists.",
                    unique);
            }

            throw;
        }
    }

    public async Task<bool> UpdateLifecycleAsync(Guid tenantId, TenantLifecycle lifecycle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lifecycle);
        var id = tenantId.ToString();
        var existing = await dbContext.Tenants.SingleOrDefaultAsync(tenant => tenant.Id == id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            Logger.Warn("Tenant {TenantId} not found for a status change", tenantId);
            return false;
        }

        existing.Status = lifecycle.Status.ToString();
        existing.StatusReason = lifecycle.Reason;
        existing.StatusChangedAt = lifecycle.ChangedAt;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var id = tenantId.ToString();
        var existing = await dbContext.Tenants.SingleOrDefaultAsync(tenant => tenant.Id == id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return false;
        }

        // Profiles, units and their links go with it (cascade).
        dbContext.Tenants.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Logger.Info("Deleted tenant {TenantId}", tenantId);
        return true;
    }

    // The DbContext is scoped to the Blazor circuit, so a failed insert must not
    // stay tracked: a stale 'Added' entry would be re-sent by any later retry
    // from the same user session.
    private void DetachFailedInsert(TenantDbModel dbModel)
    {
        DetachTracked(dbModel);
    }

    private void DetachTracked(TenantDbModel dbModel)
    {
        dbContext.Entry(dbModel).State = EntityState.Detached;
    }

    private void DetachTracked(IEnumerable<ProductiveUnitDbModel> dbModels)
    {
        foreach (var dbModel in dbModels)
        {
            dbContext.Entry(dbModel).State = EntityState.Detached;
        }
    }

    private void DetachTracked(IEnumerable<ProfileDbModel> dbModels)
    {
        foreach (var dbModel in dbModels)
        {
            dbContext.Entry(dbModel).State = EntityState.Detached;
        }
    }

    private void DetachTracked(IEnumerable<ProductiveUnitProfileDbModel> dbModels)
    {
        foreach (var dbModel in dbModels)
        {
            dbContext.Entry(dbModel).State = EntityState.Detached;
        }
    }

    private static ProductiveUnitProfileDbModel[] BuildProductiveUnitProfiles(
        IReadOnlyCollection<TenantProductiveUnitCreateModel> productiveUnits,
        IReadOnlyCollection<ProductiveUnitDbModel> productiveUnitDbModels,
        IReadOnlyDictionary<string, string> temporaryProfileIdMap)
    {
        return productiveUnits
            .Zip(productiveUnitDbModels)
            .SelectMany(pair => pair.First.ProfileIds
                .Concat(pair.First.TemporaryProfileIds.Select(temporaryProfileId =>
                    temporaryProfileIdMap.TryGetValue(temporaryProfileId, out var profileId)
                        ? profileId
                        : throw new InvalidOperationException(
                            "A productive unit references a profile temporary id that was not created in the same request.")))
                .Distinct(StringComparer.Ordinal)
                .Select(profileId => new ProductiveUnitProfileDbModel
                {
                    ProductiveUnitId = pair.Second.Id,
                    ProfileId = profileId
                }))
            .ToArray();
    }

    // PostgreSQL SQLSTATE for unique_violation.
    private const string UniqueViolationSqlState = "23505";

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: UniqueViolationSqlState
        };

    private static bool IsProductiveUnitNameUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: UniqueViolationSqlState,
            ConstraintName: "IX_ProductiveUnits_TenantId_Name"
        };
}
