using NLog;
using PSAAS.Application.Abstractions.Geography;
using PSAAS.Application.Contracts.Tenants;

namespace PSAAS.Application.Validators.Tenants;

/// <summary>UC3-R5 / UC4: Italian address, the city must exist and the postal code must be one of its own.</summary>
public sealed class TenantAddressValidator(IItalianCityProvider cities)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <returns>The official city name when the city list is reachable, otherwise the typed one.</returns>
    public async Task<string> ValidateAsync(
        string street,
        string city,
        string postalCode,
        Dictionary<string, string> errors,
        CancellationToken cancellationToken)
    {
        AddRequiredError(errors, "street", street);
        AddRequiredError(errors, "city", city);
        AddRequiredError(errors, "postalCode", postalCode);
        if (city.Length == 0)
        {
            return city;
        }

        var matches = await cities.SearchAsync(city, cancellationToken);
        if (matches is null)
        {
            Logger.Warn("Italian city list unreachable: address of {City} accepted without checking it", city);
            // shortcut: city list unreachable, the address is accepted unchecked; add a retry/queue if this becomes common.
            return city;
        }

        // Several towns share a name (e.g. Calliano TN/AT): the postal code decides.
        var sameName = matches.Where(match => match.Name.Equals(city, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (sameName.Length == 0)
        {
            errors["city"] = TenantFieldValidationErrorCodes.InvalidValue;
            return city;
        }

        var match = sameName.FirstOrDefault(candidate => candidate.PostalCodes.Contains(postalCode));
        if (match is null && postalCode.Length > 0)
        {
            errors["postalCode"] = TenantFieldValidationErrorCodes.InvalidValue;
        }

        return (match ?? sameName[0]).Name;
    }

    private static void AddRequiredError(Dictionary<string, string> errors, string field, string value)
    {
        if (value.Length == 0)
        {
            errors[field] = TenantFieldValidationErrorCodes.Required;
        }
    }
}
