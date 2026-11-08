using PSAAS.Application.Contracts.Tenants;
using PSAAS.Domain;

namespace PSAAS.Application.Validators.Tenants;

public static class TenantKinds
{
    /// <summary>Missing means Business; an unknown name is InvalidValue on "kind" (and Business is assumed).</summary>
    public static TenantKind Parse(string? value, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TenantKind.Business;
        }

        // Names only: Enum.TryParse would also take "1".
        if (!char.IsDigit(value.Trim()[0])
            && Enum.TryParse<TenantKind>(value.Trim(), ignoreCase: true, out var kind) && Enum.IsDefined(kind))
        {
            return kind;
        }

        errors["kind"] = TenantFieldValidationErrorCodes.InvalidValue;
        return TenantKind.Business;
    }

    /// <summary>The national VAT number (complete one without the country) or, for an individual, the tax code itself.</summary>
    public static string NationalPart(string completeVatNumber, TenantKind kind) =>
        kind == TenantKind.Individual ? completeVatNumber : completeVatNumber[2..];
}
