namespace PSAAS.Application.Abstractions.Tenants;

/// <summary>
/// Write-side model for productive units created together with a tenant.
/// Profile ids are existing persisted ids; temporary profile ids are resolved
/// to newly persisted profile ids by the write repository in the same transaction.
/// </summary>
public sealed record TenantProductiveUnitCreateModel(
    string Name,
    string? Description,
    IReadOnlyCollection<string> ProfileIds,
    IReadOnlyCollection<string> TemporaryProfileIds);

/// <summary>
/// Write-side model for profiles created by the tenant creation wizard.
/// </summary>
public sealed record TenantProfileCreateModel(
    string TemporaryId,
    string Name,
    string? Description);
