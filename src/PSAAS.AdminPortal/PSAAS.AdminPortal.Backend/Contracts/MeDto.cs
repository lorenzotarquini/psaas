namespace PSAAS.AdminPortal.Backend.Contracts;

/// <summary>The authenticated user as described by the Keycloak access token.</summary>
public sealed record MeDto(string? KeycloakId, string? Name, string? Email, IReadOnlyList<string> Roles);
