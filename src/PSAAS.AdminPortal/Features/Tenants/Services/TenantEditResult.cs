namespace PSAAS.AdminPortal.Features.Tenants.Services;

/// <summary>
/// Outcome of a tenant update attempt: field-level error codes use the same
/// codes as the field validation, so the UI can bind them to per-field messages.
/// </summary>
public sealed record TenantEditResult(
    bool Succeeded,
    string? CompanyNameErrorCode,
    string? CompleteVatNumberErrorCode,
    string? CertifiedEmailErrorCode,
    string? StreetErrorCode,
    string? CityErrorCode,
    string? PostalCodeErrorCode,
    string? CountryErrorCode,
    string? ErrorCode)
{
    public static TenantEditResult Updated() =>
        new(true, null, null, null, null, null, null, null, null);

    public static TenantEditResult Invalid(
        string? companyNameErrorCode = null,
        string? completeVatNumberErrorCode = null,
        string? certifiedEmailErrorCode = null,
        string? streetErrorCode = null,
        string? cityErrorCode = null,
        string? postalCodeErrorCode = null,
        string? countryErrorCode = null) =>
        new(
            false,
            companyNameErrorCode,
            completeVatNumberErrorCode,
            certifiedEmailErrorCode,
            streetErrorCode,
            cityErrorCode,
            postalCodeErrorCode,
            countryErrorCode,
            null);

    public static TenantEditResult Failed(string errorCode) =>
        new(false, null, null, null, null, null, null, null, errorCode);
}
