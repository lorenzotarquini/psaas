using System.Globalization;
using NLog;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Contracts.Users;
using PSAAS.Application.Validators.Tenants;

namespace PSAAS.Application.Services.Users;

/// <summary>
/// UC6: creates a user in the identity provider, which emails them to set the password.
/// Nothing is created unless every value is valid; errors are reported per field.
/// </summary>
public sealed class UserCreateService(
    IProfileReadRepository profiles,
    IUserDirectory directory,
    IAuditLog audit,
    ISubscriptionRepository subscriptions,
    ITenantReadRepository tenants)
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

        if (errors.Count == 0)
        {
            await CheckUserLimitsAsync(profileIds, existingProfiles, errors, cancellationToken);
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

        var user = UserReadModel.From(created, existingProfiles);
        await RecordAsync(user, cancellationToken);
        Logger.Info("Created user {UserId} with {ProfileCount} profiles", created.Id, created.ProfileIds.Count);
        return new UserCreateResult(user, errors);
    }

    // One event per tenant whose profiles the user got, so each tenant history shows it (mock 1b); none: platform-wide.
    private async Task RecordAsync(UserReadModel user, CancellationToken cancellationToken)
    {
        var name = $"{user.FirstName} {user.LastName}";
        var byTenant = user.Profiles.GroupBy(profile => profile.TenantId).ToArray();
        if (byTenant.Length == 0)
        {
            await audit.RecordAsync(
                new AuditRecord(AuditActions.UserCreated, AuditTargets.User, user.Id, name, null, new { email = user.Email, profiles = Array.Empty<string>() }),
                cancellationToken);
            return;
        }

        foreach (var tenant in byTenant)
        {
            await audit.RecordAsync(
                new AuditRecord(
                    AuditActions.UserCreated,
                    AuditTargets.User,
                    user.Id,
                    name,
                    tenant.Key,
                    new { email = user.Email, tenant = tenant.First().TenantCompanyName, profiles = tenant.Select(p => p.Name).ToArray() }),
                cancellationToken);
        }
    }

    // EXP-02: a tenant whose plan allows N users cannot get the N+1th; reported on its first chosen profile.
    private async Task CheckUserLimitsAsync(
        string[] profileIds,
        IReadOnlyDictionary<string, UserProfileReadModel> existingProfiles,
        Dictionary<string, string> errors,
        CancellationToken cancellationToken)
    {
        foreach (var tenantId in profileIds.Select(id => existingProfiles[id].TenantId).Distinct())
        {
            if (!Guid.TryParse(tenantId, out var id)) continue;
            var maxUsers = (await subscriptions.GetAsync(id, cancellationToken))?.MaxUsers;
            if (maxUsers is null) continue;
            var tenantProfiles = (await tenants.GetProfilesAndProductiveUnitsAsync(tenantId, cancellationToken)).Profiles.Select(p => p.Id).ToArray();
            if (await directory.CountUsersWithAnyAsync(tenantProfiles, cancellationToken) >= maxUsers)
            {
                var first = Array.FindIndex(profileIds, id => existingProfiles[id].TenantId == tenantId);
                errors[$"profileIds[{first}]"] = TenantFieldValidationErrorCodes.LimitExceeded;
            }
        }
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
