using Microsoft.AspNetCore.Mvc;
using NLog;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Contracts.Profiles;

namespace PSAAS.AdminPortal.Backend.Controllers;

[ApiController]
[Route("api/profiles")]
public sealed class ProfilesController(IProfileReadRepository profiles) : ControllerBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [HttpGet]
    public async Task<IReadOnlyList<ProfileReadModel>> GetAll(CancellationToken cancellationToken)
    {
        var result = await profiles.GetAllAsync(cancellationToken);
        Logger.Info("Listed {ProfileCount} profiles", result.Count);
        return result;
    }
}
