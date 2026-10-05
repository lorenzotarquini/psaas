namespace PSAAS.AdminPortal.Features.Tenants.Services;

/// <summary>
/// Outcome of a tenant creation attempt: field-level error codes use the same
/// codes as the field validation (see TenantFieldValidationErrorCodes), so the
/// UI can bind them to the existing per-field error messages.
/// </summary>
public sealed record TenantCreateResult(
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
    public static TenantCreateResult Created() =>
        new(true, null, null, null, null, null, null, null, null);

    public static TenantCreateResult Invalid(
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

    public static TenantCreateResult Failed(string errorCode) =>
        new(false, null, null, null, null, null, null, null, errorCode);
}
