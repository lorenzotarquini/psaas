using PSAAS.Application.Abstractions.Tenants;

namespace PSAAS.AdminPortal.Backend.Contracts;

public sealed record TenantDetailDto(
    Guid Id,
    string CompanyName,
    string VatNumber,
    string CompleteVatNumber,
    string CertifiedEmail,
    AddressDto Address,
    IReadOnlyList<TenantProfileDto> Profiles,
    IReadOnlyList<TenantProductiveUnitReadModel> ProductiveUnits);

/// <param name="UserCount">Users that have the profile; null when Keycloak cannot be reached.</param>
public sealed record TenantProfileDto(string Id, string Name, string? Description, int? UserCount);
