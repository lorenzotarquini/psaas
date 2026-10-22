using PSAAS.Application.Contracts.Tenants;

namespace PSAAS.AdminPortal.Backend.Contracts;

/// <summary>Outcome of checking one unique tenant field: null when valid, otherwise the code saving would return.</summary>
/// <param name="ConflictingTenant">With NotUnique: the tenant already using the value, to link to it.</param>
public sealed record TenantFieldValidationDto(string? ErrorCode, TenantReference? ConflictingTenant = null);
