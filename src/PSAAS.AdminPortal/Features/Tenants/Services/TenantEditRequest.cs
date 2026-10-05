namespace PSAAS.AdminPortal.Features.Tenants.Services;

/// <summary>
/// Application request carrying the raw tenant edit form values,
/// to be normalized and validated server-side.
/// </summary>
public sealed record TenantEditRequest(
    Guid Id,
    string? CompanyName,
    string? CompleteVatNumber,
    string? CertifiedEmail,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country);
