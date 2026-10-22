using System.Globalization;
using NLog;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Contracts.Users;
using PSAAS.Application.Validators.Tenants;

namespace PSAAS.Application.Services.Users;

/// <summary>
/// UC6: creates a user in the identity provider, which emails them to set the password.
/// Nothing is created unless every value is valid; errors are reported per field.
/// </summary>
public sealed class UserCreateService(IProfileReadRepository profiles, IUserDirectory directory)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    public async Task<UserCreateResult> CreateAsync(UserCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string>();

        var firstName = Trim(request.FirstName);
        var lastName = Trim(request.LastName);
        AddRequiredError(errors, "firstName", firstName);
        AddRequiredError(errors, "lastName", lastName);
        // R2: the email is also the username; Keycloak stores both lowercase.
        var email = ValidateEmail(Trim(request.Email).ToLowerInvariant(), errors);
        var birthDate = ValidateBirthDate(Trim(request.BirthDate), errors);

        var profileIds = (request.ProfileIds ?? []).Select(Trim).ToArray();
        var existingProfiles = (await profiles.GetWithTenantAsync(profileIds, cancellationToken))
            .ToDictionary(profile => profile.Id, StringComparer.Ordinal);
        for (var i = 0; i < profileIds.Length; i++)
        {
            if (!existingProfiles.ContainsKey(profileIds[i]))
            {
                errors[$"profileIds[{i}]"] = TenantFieldValidationErrorCodes.InvalidValue;
            }
        }

        if (errors.Count > 0)
        {
            Logger.Info("User creation rejected: {ValidationErrors}", errors);
            return new UserCreateResult(null, errors);
        }

        DirectoryUser created;
        try
        {
            created = await directory.CreateAsync(
                new NewDirectoryUser(firstName, lastName, email, birthDate!.Value, profileIds.Distinct(StringComparer.Ordinal).ToArray()),
                cancellationToken);
        }
        catch (UserEmailConflictException)
        {
            errors["email"] = TenantFieldValidationErrorCodes.NotUnique;
            Logger.Info("User creation rejected: {ValidationErrors}", errors);
            return new UserCreateResult(null, errors);
        }

        Logger.Info("Created user {UserId} with {ProfileCount} profiles", created.Id, created.ProfileIds.Count);
        return new UserCreateResult(UserReadModel.From(created, existingProfiles), errors);
    }

    private static string ValidateEmail(string email, Dictionary<string, string> errors)
    {
        if (email.Length == 0)
        {
            errors["email"] = TenantFieldValidationErrorCodes.Required;
        }
        else if (!TenantCreateValidator.IsValidEmailAddress(email))
        {
            errors["email"] = TenantFieldValidationErrorCodes.InvalidFormat;
        }

        return email;
    }

    // R3: any valid calendar date.
    private static DateOnly? ValidateBirthDate(string birthDate, Dictionary<string, string> errors)
    {
        if (birthDate.Length == 0)
        {
            errors["birthDate"] = TenantFieldValidationErrorCodes.Required;
            return null;
        }

        if (DateOnly.TryParseExact(birthDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        errors["birthDate"] = TenantFieldValidationErrorCodes.InvalidFormat;
        return null;
    }

    private static void AddRequiredError(Dictionary<string, string> errors, string field, string value)
    {
        if (value.Length == 0)
        {
            errors[field] = TenantFieldValidationErrorCodes.Required;
        }
    }

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;
}
