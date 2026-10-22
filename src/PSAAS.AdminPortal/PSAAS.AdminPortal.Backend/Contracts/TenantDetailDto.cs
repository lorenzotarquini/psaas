using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Domain;

namespace PSAAS.AdminPortal.Backend.Contracts;

public sealed record TenantDetailDto(
    Guid Id,
    string CompanyName,
    string VatNumber,
    string CompleteVatNumber,
    string CertifiedEmail,
    AddressDto Address,
    IReadOnlyList<TenantProfileDto> Profiles,
    IReadOnlyList<TenantProductiveUnitReadModel> ProductiveUnits,
    string Code,
    TenantLifecycleDto Lifecycle,
    string? SdiCode,
    string? AccountManager);

/// <param name="Status">TenantStatus name: Onboarding, Trial, Active, Suspended, Cancelling, Ceased.</param>
public sealed record TenantLifecycleDto(string Status, string? Reason, DateTimeOffset ChangedAt, DateTimeOffset CreatedAt)
{
    public static TenantLifecycleDto From(TenantLifecycle lifecycle) =>
        new(lifecycle.Status.ToString(), lifecycle.Reason, lifecycle.ChangedAt, lifecycle.CreatedAt);
}

/// <param name="UserCount">Users that have the profile; null when Keycloak cannot be reached.</param>
public sealed record TenantProfileDto(string Id, string Name, string? Description, int? UserCount);
