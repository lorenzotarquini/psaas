namespace PSAAS.Application.Contracts.Tenants;

public sealed record TenantFieldValidationResult(
    bool IsValid,
    string? ErrorCode,
    string NormalizedValue)
{
    public static TenantFieldValidationResult Valid(string normalizedValue)
    {
        return new TenantFieldValidationResult(true, null, normalizedValue);
    }

    public static TenantFieldValidationResult Invalid(string errorCode, string normalizedValue = "")
    {
        return new TenantFieldValidationResult(false, errorCode, normalizedValue);
    }
}
