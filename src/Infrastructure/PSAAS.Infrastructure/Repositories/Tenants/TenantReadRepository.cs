using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Domain;
using PSAAS.Infrastructure.Converters;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Repositories.Tenants;

public sealed class TenantReadRepository(
    PsaasDbContext dbContext,
    ILogger<TenantReadRepository>? logger = null) : ITenantReadRepository
{
    private readonly ILogger<TenantReadRepository> _logger = logger ?? NullLogger<TenantReadRepository>.Instance;

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Querying all tenants");
        var tenants = await dbContext.Tenants
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Queried {TenantCount} tenants", tenants.Count);

        return tenants
            .Select(TenantDbModelConverter.ToDomain)
            .ToArray();
    }

    public async Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Querying tenant {TenantId}", tenantId);
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(
                tenant => tenant.Id == tenantId.ToString(),
                cancellationToken)
            .ConfigureAwait(false);

        return tenant is null ? null : TenantDbModelConverter.ToDomain(tenant);
    }

    public async Task<TenantStructureReadModel> GetProfilesAndProductiveUnitsAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Querying structure of tenant {TenantId}", tenantId);
        var profiles = await dbContext.Profiles
            .AsNoTracking()
            .Where(profile => profile.TenantId == tenantId)
            .OrderBy(profile => profile.Name)
            .Select(profile => new TenantProfileReadModel(profile.Id, profile.Name, profile.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var units = await dbContext.ProductiveUnits
            .AsNoTracking()
            .Where(unit => unit.TenantId == tenantId)
            .OrderBy(unit => unit.Name)
            .Select(unit => new
            {
                unit.Id,
                unit.Name,
                unit.Description,
                ProfileIds = unit.ProductiveUnitProfiles.Select(link => link.ProfileId).ToList()
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new TenantStructureReadModel(
            profiles,
            units.Select(unit => new TenantProductiveUnitReadModel(unit.Id, unit.Name, unit.Description, unit.ProfileIds)).ToList());
    }
}
