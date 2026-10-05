namespace PSAAS.AdminPortal.Features.Tenants.Services;

/// <summary>
/// Application service exposing tenant projections to the Tenants feature UI,
/// so that views and view models do not depend on the domain model.
/// </summary>
public interface ITenantService
{
    Task<IReadOnlyList<TenantListItem>> GetTenantListAsync(CancellationToken cancellationToken = default);
}
