namespace PSAAS.Application.Contracts.Tenants;

public static class TenantFieldValidationErrorCodes
{
    public const string Required = "Required";
    public const string InvalidFormat = "InvalidFormat";
    public const string MaxLengthExceeded = "MaxLengthExceeded";
    public const string NotUnique = "NotUnique";
}
