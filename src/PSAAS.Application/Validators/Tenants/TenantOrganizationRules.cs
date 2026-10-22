using PSAAS.Application.Contracts.Tenants;

namespace PSAAS.Application.Validators.Tenants;

/// <summary>
/// Rules on profiles and productive units shared by UC3 (creation) and UC5 (edit), with errors by position
/// (e.g. "profiles[1].name", "productiveUnits[0].profileKeys[2]").
/// </summary>
public static class TenantOrganizationRules
{
    /// <summary>UC3-R6 / UC5-R1: names required and unique within the request, ignoring case.</summary>
    public static void ValidateNames(IReadOnlyList<string?> names, string collection, Dictionary<string, string> errors)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < names.Count; i++)
        {
            var name = Trim(names[i]);
            if (name.Length == 0)
            {
                errors[$"{collection}[{i}].name"] = TenantFieldValidationErrorCodes.Required;
            }
            else if (!seen.Add(name))
            {
                errors[$"{collection}[{i}].name"] = TenantFieldValidationErrorCodes.NotUnique;
            }
        }
    }

    /// <returns>The keys of the profiles of the request; a repeated key is NotUnique.</returns>
    public static HashSet<string> ValidateProfileKeys(IReadOnlyList<string?> keys, Dictionary<string, string> errors)
    {
        var known = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < keys.Count; i++)
        {
            var key = Trim(keys[i]);
            if (key.Length > 0 && !known.Add(key))
            {
                errors[$"profiles[{i}].key"] = TenantFieldValidationErrorCodes.NotUnique;
            }
        }

        return known;
    }

    /// <summary>Every unit must reference profiles of the same request by key.</summary>
    public static void ValidateUnitProfileKeys(
        IReadOnlyList<IReadOnlyList<string>?> unitProfileKeys,
        HashSet<string> profileKeys,
        Dictionary<string, string> errors)
    {
        for (var i = 0; i < unitProfileKeys.Count; i++)
        {
            var keys = unitProfileKeys[i] ?? [];
            for (var j = 0; j < keys.Count; j++)
            {
                if (!profileKeys.Contains(Trim(keys[j])))
                {
                    errors[$"productiveUnits[{i}].profileKeys[{j}]"] = TenantFieldValidationErrorCodes.InvalidValue;
                }
            }
        }
    }

    public static string Trim(string? value) => value?.Trim() ?? string.Empty;

    public static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
