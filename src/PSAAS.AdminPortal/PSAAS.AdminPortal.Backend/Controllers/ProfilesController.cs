using Microsoft.AspNetCore.Mvc;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Contracts.Profiles;

namespace PSAAS.AdminPortal.Backend.Controllers;

[ApiController]
[Route("api/profiles")]
public sealed class ProfilesController(IProfileReadRepository profiles) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ProfileReadModel>> GetAll(CancellationToken cancellationToken) =>
        profiles.GetAllAsync(cancellationToken);
}
