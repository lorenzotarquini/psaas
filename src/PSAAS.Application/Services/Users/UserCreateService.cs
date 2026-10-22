using NLog;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Contracts.Users;

namespace PSAAS.Application.Services.Users;

/// <summary>
/// UC6: creates a user in the identity provider, which emails them to set the password.
/// Nothing is created unless every value is valid; errors are reported per field.
/// </summary>
public sealed class UserCreateService(UserRules rules, IUserDirectory directory, ICurrentActor actor, TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>The values UC6 would save, or the errors it would report (UC20 preview); creates nothing.</summary>
    public async Task<(NewDirectoryUser? User, IReadOnlyDictionary<string, UserProfileReadModel> Profiles, Dictionary<string, string> Errors)> ValidateAsync(
        UserCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string>();
        var firstName = UserRules.Trim(request.FirstName);
        var lastName = UserRules.Trim(request.LastName);
        UserRules.Required(errors, "firstName", firstName);
        UserRules.Required(errors, "lastName", lastName);
        var email = UserRules.ValidateEmail(request.Email, errors);
        var birthDate = UserRules.ValidateBirthDate(request.BirthDate, errors);
        var profileIds = (request.ProfileIds ?? []).Select(UserRules.Trim).ToArray();
        var existingProfiles = await rules.ValidateProfilesAsync(profileIds, errors, null, cancellationToken);

        var user = errors.Count > 0
            ? null
            : new NewDirectoryUser(
                firstName, lastName, email, birthDate!.Value, profileIds.Distinct(StringComparer.Ordinal).ToArray(),
                actor.Name, clock.GetUtcNow());
        return (user, existingProfiles, errors);
    }

    /// <exception cref="UserDirectoryUnavailableException">The identity provider cannot be reached.</exception>
    public async Task<UserCreateResult> CreateAsync(UserCreateRequest request, CancellationToken cancellationToken = default)
    {
        var (newUser, existingProfiles, errors) = await ValidateAsync(request, cancellationToken);
        if (newUser is null)
        {
            Logger.Info("User creation rejected: {ValidationErrors}", errors);
            return new UserCreateResult(null, errors);
        }

        DirectoryUser created;
        try
        {
            created = await directory.CreateAsync(newUser, cancellationToken);
        }
        catch (UserEmailConflictException)
        {
            errors["email"] = TenantFieldValidationErrorCodes.NotUnique;
            Logger.Info("User creation rejected: {ValidationErrors}", errors);
            return new UserCreateResult(null, errors);
        }

        var user = UserReadModel.From(created, existingProfiles);
        await rules.RecordAsync(
            AuditActions.UserCreated,
            user,
            profiles => profiles.Count == 0
                ? new { email = user.Email, profiles = Array.Empty<string>() }
                : new { email = user.Email, tenant = profiles[0].TenantCompanyName, profiles = profiles.Select(p => p.Name).ToArray() },
            cancellationToken);
        Logger.Info("Created user {UserId} with {ProfileCount} profiles", created.Id, created.ProfileIds.Count);
        return new UserCreateResult(user, errors);
    }
}
