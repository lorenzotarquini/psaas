using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NLog;
using FinHorizon.MySync.Backend.Contracts;

namespace FinHorizon.MySync.Backend.Controllers;

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
        Logger.Info("Resolved current user {KeycloakId}", dto.KeycloakId);
        return dto;
    }
}
