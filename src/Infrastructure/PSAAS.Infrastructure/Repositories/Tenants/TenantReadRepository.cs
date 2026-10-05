using Microsoft.EntityFrameworkCore;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Domain;
using PSAAS.Infrastructure.Converters;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Repositories.Tenants;

public sealed class TenantReadRepository(PsaasDbContext dbContext) : ITenantReadRepository
{
    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await dbContext.Tenants
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return tenants
            .Select(TenantDbModelConverter.ToDomain)
            .ToArray();
    }

    public async Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(
                tenant => tenant.Id == tenantId.ToString(),
                cancellationToken)
            .ConfigureAwait(false);

        return tenant is null ? null : TenantDbModelConverter.ToDomain(tenant);
    }
}
