using PSAAS.Application.Abstractions.Tenants;

namespace PSAAS.AdminPortal.Backend.Contracts;

public sealed record TenantDetailDto(
    Guid Id,
    string CompanyName,
    string VatNumber,
    string CompleteVatNumber,
    string CertifiedEmail,
    AddressDto Address,
    IReadOnlyList<TenantProfileReadModel> Profiles,
    IReadOnlyList<TenantProductiveUnitReadModel> ProductiveUnits);
