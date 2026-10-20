namespace PSAAS.Application.Abstractions.Tenants;

public sealed record TenantStructureReadModel(
    IReadOnlyList<TenantProfileReadModel> Profiles,
    IReadOnlyList<TenantProductiveUnitReadModel> ProductiveUnits);

public sealed record TenantProfileReadModel(string Id, string Name, string? Description);

public sealed record TenantProductiveUnitReadModel(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<string> ProfileIds);

/// <summary>Profile to persist on tenant edit; null Id means new, ClientId links units to profiles.</summary>
public sealed record TenantProfileEditModel(string? Id, Guid ClientId, string Name, string? Description);

public sealed record TenantProductiveUnitEditModel(
    Guid? Id,
    string Name,
    string? Description,
    IReadOnlyList<Guid> ProfileClientIds);
