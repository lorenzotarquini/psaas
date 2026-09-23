using Microsoft.EntityFrameworkCore;
using PSAAS.Application.Features.Tenants;
using PSAAS.Domain;
using PSAAS.Infrastructure.Converters;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Features.Tenants.Data;

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
}
