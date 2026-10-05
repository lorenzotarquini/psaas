namespace PSAAS.AdminPortal.Features.Tenants.Services;

/// <summary>
/// Read-only projection of a tenant consumed by the Tenants feature UI.
/// </summary>
public sealed record TenantListItem(
    Guid Id,
    string CompanyName,
    string VatNumber,
    string CertifiedEmail);
