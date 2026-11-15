namespace FinHorizon.MySync.Backend.Contracts;

/// <summary>The signed-in user, from the Keycloak token.</summary>
public sealed record MeDto(string? KeycloakId, string? Name, string? Email, IReadOnlyList<string> Roles);
