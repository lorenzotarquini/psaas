namespace PSAAS.Application.Contracts.Profiles;

/// <summary>
/// Read model used by application features that need to list the profiles
/// already configured in the system.
/// </summary>
public sealed record ProfileReadModel(
    string Id,
    string TenantId,
    string TenantCompanyName,
    string Name,
    string? Description);
