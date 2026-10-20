using PSAAS.Application.Contracts.Users;

namespace PSAAS.AdminPortal.Backend.Contracts;

/// <param name="User">Null when the authenticated account is not registered in PSAAS (e.g. a platform administrator).</param>
public sealed record MeDto(string? Name, IReadOnlyList<string> Roles, UserReadModel? User);
