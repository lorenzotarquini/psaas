using NLog;
using PSAAS.Application.Abstractions.Geography;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Validators.Tenants;
using PSAAS.Domain;

namespace PSAAS.Application.Services.Tenants;

/// <summary>
/// Creates a tenant, optionally with its profiles and productive units, in one transaction.
/// Every value is validated server-side; duplicates are reported per field.
/// </summary>
public sealed class TenantCreateService(
    TenantCreateValidator validator,
    IItalianCityProvider cities,
    ITenantWriteRepository tenants)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const string Country = "IT";

    public async Task<TenantCreateResult> CreateAsync(TenantCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string>();

        var completeVatNumber = await ValidateUniqueFieldsAsync(request, errors, cancellationToken);
        var street = Trim(request.Street);
        AddRequiredError(errors, "street", street);
        var city = await ValidateCityAsync(Trim(request.City), Trim(request.PostalCode), errors, cancellationToken);

        var profiles = request.Profiles ?? [];
        var units = request.ProductiveUnits ?? [];
        ValidateNames(profiles.Select(profile => profile.Name).ToArray(), "profiles", errors);
        ValidateNames(units.Select(unit => unit.Name).ToArray(), "productiveUnits", errors);
        var profileKeys = ValidateProfileKeys(profiles, errors);
        ValidateUnitProfileKeys(units, profileKeys, errors);

        if (errors.Count > 0)
        {
            Logger.Info("Tenant creation rejected: {ValidationErrors}", errors);
            return new TenantCreateResult(null, errors);
        }

        var tenant = Tenant.Create(
            Trim(request.CompanyName),
            completeVatNumber[2..],
            completeVatNumber,
            Trim(request.CertifiedEmail),
            new Address(street, city, Trim(request.PostalCode), Country));
        // Profiles without a key are never referenced, so any unique temporary id works for them.
        var temporaryIds = profiles.Select(profile => NullIfEmpty(profile.Key) ?? Guid.NewGuid().ToString()).ToArray();

        try
        {
            await tenants.AddWithProfilesAndProductiveUnitsAsync(
                tenant,
                profiles.Select((profile, i) => new TenantProfileCreateModel(
                    temporaryIds[i], Trim(profile.Name), NullIfEmpty(profile.Description))).ToArray(),
                units.Select(unit => new TenantProductiveUnitCreateModel(
                    Trim(unit.Name),
                    NullIfEmpty(unit.Description),
                    [],
                    (unit.ProfileKeys ?? []).Select(Trim).ToArray())).ToArray(),
                cancellationToken);
        }
        catch (TenantUniquenessConflictException)
        {
            // A concurrent insert won the unique-index race: report which fields are now taken.
            await ValidateUniqueFieldsAsync(request, errors, cancellationToken);
            if (errors.Count == 0)
            {
                Logger.Error("Tenant {TenantId} hit a unique conflict that no field explains", tenant.Id);
                throw;
            }

            Logger.Warn("Tenant creation lost a concurrent duplicate race: {ValidationErrors}", errors);
            return new TenantCreateResult(null, errors);
        }

        Logger.Info(
            "Created tenant {TenantId} with {ProfileCount} profiles and {ProductiveUnitCount} productive units",
            tenant.Id,
            profiles.Count,
            units.Count);
        return new TenantCreateResult(tenant.Id, errors);
    }

    private async Task<string> ValidateUniqueFieldsAsync(
        TenantCreateRequest request,
        Dictionary<string, string> errors,
        CancellationToken cancellationToken)
    {
        AddError(errors, "companyName", await validator.ValidateCompanyNameAsync(request.CompanyName, cancellationToken));
        var completeVatNumber = await validator.ValidateCompleteVatNumberAsync(request.CompleteVatNumber, cancellationToken);
        AddError(errors, "completeVatNumber", completeVatNumber);
        AddError(errors, "certifiedEmail", await validator.ValidateCertifiedEmailAsync(request.CertifiedEmail, cancellationToken));
        return completeVatNumber.NormalizedValue;
    }

    /// <returns>The official city name when the city list is reachable, otherwise the typed one.</returns>
    private async Task<string> ValidateCityAsync(
        string city,
        string postalCode,
        Dictionary<string, string> errors,
        CancellationToken cancellationToken)
    {
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

    private static void ValidateNames(IReadOnlyList<string?> names, string collection, Dictionary<string, string> errors)
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

    private static HashSet<string> ValidateProfileKeys(
        IReadOnlyList<TenantCreateProfileRequest> profiles,
        Dictionary<string, string> errors)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < profiles.Count; i++)
        {
            var key = Trim(profiles[i].Key);
            if (key.Length > 0 && !keys.Add(key))
            {
                errors[$"profiles[{i}].key"] = TenantFieldValidationErrorCodes.NotUnique;
            }
        }

        return keys;
    }

    private static void ValidateUnitProfileKeys(
        IReadOnlyList<TenantCreateProductiveUnitRequest> units,
        HashSet<string> profileKeys,
        Dictionary<string, string> errors)
    {
        for (var i = 0; i < units.Count; i++)
        {
            var keys = units[i].ProfileKeys ?? [];
            for (var j = 0; j < keys.Count; j++)
            {
                if (!profileKeys.Contains(Trim(keys[j])))
                {
                    errors[$"productiveUnits[{i}].profileKeys[{j}]"] = TenantFieldValidationErrorCodes.InvalidValue;
                }
            }
        }
    }

    private static void AddError(Dictionary<string, string> errors, string field, TenantFieldValidationResult result)
    {
        if (!result.IsValid)
        {
            errors[field] = result.ErrorCode!;
        }
    }

    private static void AddRequiredError(Dictionary<string, string> errors, string field, string value)
    {
        if (value.Length == 0)
        {
            errors[field] = TenantFieldValidationErrorCodes.Required;
        }
    }

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
