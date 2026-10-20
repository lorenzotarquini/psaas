using PSAAS.Domain;

namespace PSAAS.AdminPortal.Backend.Contracts;

public sealed record TenantSummaryDto(
    Guid Id,
    string CompanyName,
    string VatNumber,
    string CompleteVatNumber,
    string CertifiedEmail,
    string City)
{
    public static TenantSummaryDto From(Tenant tenant) => new(
        tenant.Id,
        tenant.CompanyName,
        tenant.VatNumber,
        tenant.CompleteVatNumber,
        tenant.CertifiedEmail,
        tenant.Address.city);
}
