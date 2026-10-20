using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NLog;
using PSAAS.AdminPortal.Backend.Contracts;
using PSAAS.Application.Abstractions.Users;

namespace PSAAS.AdminPortal.Backend.Controllers;

[ApiController]
[Route("api/me")]
public sealed class MeController(IUserReadRepository users) : ControllerBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [HttpGet]
    public async Task<MeDto> Get(CancellationToken cancellationToken)
    {
        var keycloakId = User.FindFirstValue("sub");
        var user = keycloakId is null ? null : await users.GetByKeycloakIdAsync(keycloakId, cancellationToken);
        Logger.Info(
            "Resolved current user {KeycloakId}: registered {IsRegistered}, tenant {TenantId}",
            keycloakId,
            user is not null,
            user?.TenantId);

        return new MeDto(
            User.Identity?.Name,
            User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct().ToArray(),
            user);
    }
}
