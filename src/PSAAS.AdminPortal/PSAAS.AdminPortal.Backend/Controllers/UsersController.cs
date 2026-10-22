using Microsoft.AspNetCore.Mvc;
using NLog;
using PSAAS.Application.Contracts.Users;
using PSAAS.Application.Services.Users;

namespace PSAAS.AdminPortal.Backend.Controllers;

// Keycloak unreachable (UserDirectoryUnavailableException) becomes 503 in Program.cs.
[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [HttpGet]
    public Task<IReadOnlyList<UserReadModel>> Search(
        [FromQuery] string? search,
        [FromServices] UserSearchService searchService,
        CancellationToken cancellationToken) =>
        searchService.SearchAsync(search, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<UserReadModel>> Create(
        UserCreateRequest request,
        [FromServices] UserCreateService createService,
        CancellationToken cancellationToken)
    {
        var result = await createService.CreateAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            // Field details are logged by UserCreateService.
            Logger.Info("User creation answered 400 with {InvalidFieldCount} invalid fields", result.Errors.Count);
            return ValidationProblem(new ValidationProblemDetails(
                result.Errors.ToDictionary(error => error.Key, error => new[] { error.Value })));
        }

        // No user detail endpoint (UC6/UC7 only), hence no Location.
        return StatusCode(StatusCodes.Status201Created, result.User);
    }
}
