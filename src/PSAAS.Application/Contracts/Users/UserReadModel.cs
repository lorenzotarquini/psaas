namespace PSAAS.Application.Contracts.Users;

public sealed record UserReadModel(
    Guid Id,
    string KeycloakId,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string? TenantId,
    string? ProfileId,
    string? ProfileName);
