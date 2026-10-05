namespace PSAAS.AdminPortal.Features.Tenants.Services;

/// <summary>
/// Feature-scoped application service that validates and persists a new tenant.
/// </summary>
public interface ITenantCreateService
{
    Task<TenantCreateResult> CreateTenantAsync(
        TenantCreateRequest request,
        CancellationToken cancellationToken = default);
}
