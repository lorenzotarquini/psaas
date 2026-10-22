namespace PSAAS.Application.Contracts.Tenants;

/// <summary>UC4: the whole registry of an existing tenant, VAT number included (same rules as UC3).</summary>
public sealed record TenantRegistryUpdateRequest(
    string? CompanyName,
    string? CompleteVatNumber,
    string? CertifiedEmail,
    string? Street,
    string? City,
    string? PostalCode);

/// <summary>
/// UC5: replaces the whole organization of a tenant; what is not in the request is removed.
/// </summary>
public sealed record TenantOrganizationRequest(
    IReadOnlyList<TenantOrganizationProfileRequest>? Profiles,
    IReadOnlyList<TenantOrganizationUnitRequest>? ProductiveUnits);

/// <param name="Id">An existing profile of the tenant (kept, maybe renamed), or null for a new one.</param>
/// <param name="Key">Client-chosen key the units of the same request use to reference this profile.</param>
public sealed record TenantOrganizationProfileRequest(string? Id, string? Key, string? Name, string? Description);

/// <param name="Id">An existing productive unit of the tenant, or null for a new one.</param>
public sealed record TenantOrganizationUnitRequest(
    Guid? Id,
    string? Name,
    string? Description,
    IReadOnlyList<string>? ProfileKeys = null);

/// <summary>Outcome of an edit: not found, invalid (errors by field path) or saved.</summary>
public sealed record TenantEditResult(bool Found, IReadOnlyDictionary<string, string> Errors)
{
    public static readonly TenantEditResult NotFound = new(false, new Dictionary<string, string>());

    public bool Succeeded => Found && Errors.Count == 0;
}
