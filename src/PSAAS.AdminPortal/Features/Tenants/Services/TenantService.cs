using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Domain;

namespace PSAAS.AdminPortal.Features.Tenants.Services;

public sealed class TenantService(ITenantReadRepository tenantReadRepository) : ITenantService
{
    public async Task<IReadOnlyList<TenantListItem>> GetTenantListAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await tenantReadRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        return tenants
            .Select(ToListItem)
            .ToArray();
    }

    private static TenantListItem ToListItem(Tenant tenant) =>
        new(tenant.Id, tenant.CompanyName, tenant.VatNumber, tenant.CertifiedEmail);
}
