namespace FinHorizon.AdminPortal.Application.Contracts.Profiles;

/// <summary>UC9: a new profile of an organization, with no productive units yet.</summary>
public sealed record ProfileCreateRequest(Guid? TenantId, string? Name, string? Description);

/// <param name="Profile">The created profile; null when the tenant was not found or a field is wrong.</param>
/// <param name="Errors">Field ("tenantId", "name") to error code (see TenantFieldValidationErrorCodes).</param>
public sealed record ProfileCreateResult(ProfileReadModel? Profile, IReadOnlyDictionary<string, string> Errors)
{
    public bool Succeeded => Profile is not null;
}
