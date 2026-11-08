using Microsoft.AspNetCore.Mvc;
using NLog;
using FinHorizon.AdminPortal.Application.Abstractions.Profiles;
using FinHorizon.AdminPortal.Application.Contracts.Profiles;
using FinHorizon.AdminPortal.Application.Services.Tenants;

namespace FinHorizon.AdminPortal.Backend.Controllers;

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

    // UC9: a profile of an organization, with the UC5 rules.
    [HttpPost]
    public async Task<ActionResult<ProfileReadModel>> Create(
        ProfileCreateRequest request,
        [FromServices] ProfileService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            Logger.Info("Profile creation answered 400: {ValidationErrors}", result.Errors);
            return ValidationProblem(new ValidationProblemDetails(
                result.Errors.ToDictionary(error => error.Key, error => new[] { error.Value })));
        }

        return Created($"/api/profiles/{result.Profile!.Id}", result.Profile);
    }

    // UC9: always allowed (UC5-R4); the users that had the profile lose it.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromServices] ProfileService service, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
