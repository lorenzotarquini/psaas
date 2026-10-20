using Microsoft.AspNetCore.Mvc;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Users;

namespace PSAAS.AdminPortal.Backend.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/users")]
public sealed class TenantUsersController(ITenantReadRepository tenants, IUserReadRepository users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserReadModel>>> GetAll(Guid tenantId, CancellationToken cancellationToken)
    {
        if (await tenants.GetByIdAsync(tenantId, cancellationToken) is null)
        {
            return NotFound();
        }

        return Ok(await users.GetByTenantAsync(tenantId.ToString(), cancellationToken));
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserReadModel>> GetById(Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(tenantId.ToString(), userId, cancellationToken);
        return user is null ? NotFound() : user;
    }
}
