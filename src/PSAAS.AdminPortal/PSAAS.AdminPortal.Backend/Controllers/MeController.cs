using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NLog;
using PSAAS.AdminPortal.Backend.Contracts;

namespace PSAAS.AdminPortal.Backend.Controllers;

[ApiController]
[Route("api/me")]
public sealed class MeController : ControllerBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [HttpGet]
    public MeDto Get()
    {
        // Everything about the user lives in Keycloak: the token is the only source.
        var dto = new MeDto(
            User.FindFirstValue("sub"),
            User.Identity?.Name,
            User.FindFirstValue("email"),
            User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct().ToArray());
        Logger.Info("Resolved current user {KeycloakId} with roles {Roles}", dto.KeycloakId, dto.Roles);
        return dto;
    }
}
