using PSAAS.Application.Contracts.Users;

namespace PSAAS.Application.Contracts.Overview;

/// <summary>UC29 (mock 1b): the KPIs of the Panoramica; users are null when Keycloak could not count them.</summary>
public sealed record OverviewKpis(
    int ActiveTenants,
    int ActiveTenantsThisMonth,
    int TrialTenants,
    int TrialsEndingSoon,
    int? Users,
    int? UsersThisMonth,
    int Modules,
    int ModulesInBeta);

public sealed record SearchTenant(Guid Id, string CompanyName, string CompleteVatNumber, string CertifiedEmail, string Status);

/// <param name="UsersUnavailable">Keycloak did not answer: the users part is empty on purpose (UC30 2a).</param>
public sealed record SearchResult(IReadOnlyList<SearchTenant> Tenants, IReadOnlyList<UserReadModel> Users, bool UsersUnavailable);

/// <param name="Minutes">5–60, default 30 (UC31-R1).</param>
public sealed record ImpersonationRequest(string? Reason, int? Minutes);

/// <param name="Url">The platform address with the session in its fragment (UC31-R2).</param>
public sealed record ImpersonationTicket(string Url, DateTimeOffset ExpiresAt);

public sealed record ImpersonationResult(ImpersonationTicket? Ticket, bool Found, bool Conflict, IReadOnlyDictionary<string, string> Errors);
