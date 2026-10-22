namespace PSAAS.Application.Contracts.Tenants;

public static class TenantFieldValidationErrorCodes
{
    public const string Required = "Required";
    public const string InvalidFormat = "InvalidFormat";
    public const string MaxLengthExceeded = "MaxLengthExceeded";
    public const string NotUnique = "NotUnique";
    public const string InvalidValue = "InvalidValue";
    /// <summary>EXP-02: the change would go beyond a limit of the plan (users, productive units).</summary>
    public const string LimitExceeded = "LimitExceeded";
}
