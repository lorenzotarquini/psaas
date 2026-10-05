namespace PSAAS.AdminPortal.Features.Tenants.Services;

/// <summary>
/// Application request carrying the raw tenant creation form values,
/// to be normalized and validated server-side.
/// </summary>
/// <remarks>
/// The form has a single VAT field that carries the complete VAT number
/// (country prefix plus 11 digits, e.g. IT12345678901).
/// </remarks>
public sealed record TenantCreateRequest(
    string? CompanyName,
    string? CompleteVatNumber,
    string? CertifiedEmail,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country);
