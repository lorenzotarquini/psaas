namespace PSAAS.AdminPortal.Features.Tenants.Services;

public interface ITenantEditService
{
    Task<TenantEditReadModel?> GetTenantForEditAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<TenantEditResult> UpdateTenantAsync(
        TenantEditRequest request,
        CancellationToken cancellationToken = default);
}
