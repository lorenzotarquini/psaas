namespace PSAAS.Application.Contracts.Tenants;

/// <summary>
/// Raw tenant creation values, normalized and validated server-side.
/// The country is not requested: addresses are Italian only for now.
/// </summary>
/// <param name="CompleteVatNumber">Country prefix plus 11 digits (e.g. IT12345678901); the national part is derived from it.</param>
/// <param name="Profiles">Optional profiles created together with the tenant.</param>
/// <param name="ProductiveUnits">Optional productive units created together with the tenant.</param>
/// <param name="SdiCode">Optional SDI recipient code (6–7 letters or digits).</param>
/// <param name="AccountManager">Optional name of the account manager on the platform side.</param>
/// <param name="Draft">UC14: create the tenant in Onboarding, to complete it step by step and activate it later.</param>
public sealed record TenantCreateRequest(
    string? CompanyName,
    string? CompleteVatNumber,
    string? CertifiedEmail,
    string? Street,
    string? City,
    string? PostalCode,
    IReadOnlyList<TenantCreateProfileRequest>? Profiles = null,
    IReadOnlyList<TenantCreateProductiveUnitRequest>? ProductiveUnits = null,
    string? SdiCode = null,
    string? AccountManager = null,
    bool Draft = false);

/// <param name="Key">Client-chosen key that productive units of the same request use to reference this profile.</param>
public sealed record TenantCreateProfileRequest(string? Key, string? Name, string? Description);

/// <param name="ProfileKeys">Keys of profiles of the same request to link to this unit.</param>
public sealed record TenantCreateProductiveUnitRequest(
    string? Name,
    string? Description,
    IReadOnlyList<string>? ProfileKeys = null);

/// <param name="Errors">Field path (e.g. "companyName", "profiles[0].name") to error code (see TenantFieldValidationErrorCodes).</param>
public sealed record TenantCreateResult(Guid? TenantId, IReadOnlyDictionary<string, string> Errors)
{
    public bool Succeeded => TenantId is not null;
}
