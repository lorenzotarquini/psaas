namespace PSAAS.Application.Contracts.Users;

/// <summary>Raw user creation values (UC6), validated server-side.</summary>
/// <param name="BirthDate">"YYYY-MM-DD"; a string so that a malformed date gets a field code instead of a binding error.</param>
/// <param name="ProfileIds">Optional profiles, possibly of different tenants.</param>
public sealed record UserCreateRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? BirthDate,
    IReadOnlyList<string>? ProfileIds = null);

/// <param name="Errors">Field path (e.g. "email", "profileIds[0]") to error code (see TenantFieldValidationErrorCodes).</param>
public sealed record UserCreateResult(UserReadModel? User, IReadOnlyDictionary<string, string> Errors)
{
    public bool Succeeded => User is not null;
}
