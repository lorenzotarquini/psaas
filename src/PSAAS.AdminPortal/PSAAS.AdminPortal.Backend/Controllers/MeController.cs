using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Users;

namespace PSAAS.AdminPortal.Backend.Controllers;

[ApiController]
[Route("api/me")]
public sealed class MeController(IUserReadRepository users) : ControllerBase
{
    [HttpGet]
    public async Task<MeDto> Get(CancellationToken cancellationToken)
    {
        var keycloakId = User.FindFirstValue("sub");
        var user = keycloakId is null ? null : await users.GetByKeycloakIdAsync(keycloakId, cancellationToken);

        return new MeDto(
            User.Identity?.Name,
            User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct().ToArray(),
            user);
    }
}

/// <param name="User">Null when the authenticated account is not registered in PSAAS (e.g. a platform administrator).</param>
public sealed record MeDto(string? Name, IReadOnlyList<string> Roles, UserReadModel? User);
