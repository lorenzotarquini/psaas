using Microsoft.AspNetCore.Mvc;
using NLog;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Users;

namespace PSAAS.AdminPortal.Backend.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/users")]
public sealed class TenantUsersController(
    ITenantReadRepository tenants,
    IUserReadRepository users) : ControllerBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserReadModel>>> GetAll(Guid tenantId, CancellationToken cancellationToken)
    {
        if (await tenants.GetByIdAsync(tenantId, cancellationToken) is null)
        {
            Logger.Info("Users of tenant {TenantId} requested, but the tenant does not exist", tenantId);
            return NotFound();
        }

        var result = await users.GetByTenantAsync(tenantId.ToString(), cancellationToken);
        Logger.Info("Listed {UserCount} users of tenant {TenantId}", result.Count, tenantId);
        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserReadModel>> GetById(Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(tenantId.ToString(), userId, cancellationToken);
        if (user is null)
        {
            Logger.Info("User {UserId} not found in tenant {TenantId}", userId, tenantId);
            return NotFound();
        }

        Logger.Info("Returned user {UserId} of tenant {TenantId}", userId, tenantId);
        return user;
    }
}
