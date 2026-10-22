using Microsoft.AspNetCore.Mvc;
using NLog;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Users;
using PSAAS.Application.Services.Users;

namespace PSAAS.AdminPortal.Backend.Controllers;

// Keycloak unreachable (UserDirectoryUnavailableException) becomes 503 in Program.cs.
[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>UC7 with the filters of mock 1f.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserReadModel>>> Search(
        [FromQuery] string? search,
        [FromQuery] string? tenantId,
        [FromQuery] string? profileId,
        [FromQuery] string? status,
        [FromServices] UserSearchService searchService,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1) errors["page"] = ["InvalidValue"];
        if (pageSize is < 1 or > 200) errors["pageSize"] = ["InvalidValue"];
        if (status is not null && !UserStatuses.All.Contains(status)) errors["status"] = ["InvalidValue"];
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        return await searchService.SearchAsync(new UserQuery(search, tenantId, profileId, status, page, pageSize), cancellationToken);
    }

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
            return Problem(result.Errors);
        }

        return CreatedAtAction(nameof(Get), new { id = result.User!.Id }, result.User);
    }

    /// <summary>UC19: the side panel of mock 1f.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserReadModel>> Get(string id, [FromServices] UserManagementService users, CancellationToken cancellationToken) =>
        await users.GetAsync(id, cancellationToken) is { } user ? user : NotFound();

    [HttpPut("{id}")]
    public async Task<ActionResult<UserReadModel>> Update(
        string id,
        UserUpdateRequest request,
        [FromServices] UserManagementService users,
        CancellationToken cancellationToken) =>
        ToResponse(await users.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id}/invite")]
    public async Task<ActionResult<UserReadModel>> Invite(string id, [FromServices] UserManagementService users, CancellationToken cancellationToken) =>
        ToResponse(await users.InviteAsync(id, cancellationToken));

    [HttpPost("{id}/reset-password")]
    public async Task<ActionResult<UserReadModel>> ResetPassword(string id, [FromServices] UserManagementService users, CancellationToken cancellationToken) =>
        ToResponse(await users.ResetPasswordAsync(id, cancellationToken));

    [HttpPost("{id}/disable")]
    public async Task<ActionResult<UserReadModel>> Disable(string id, [FromServices] UserManagementService users, CancellationToken cancellationToken) =>
        ToResponse(await users.SetEnabledAsync(id, false, cancellationToken));

    [HttpPost("{id}/enable")]
    public async Task<ActionResult<UserReadModel>> Enable(string id, [FromServices] UserManagementService users, CancellationToken cancellationToken) =>
        ToResponse(await users.SetEnabledAsync(id, true, cancellationToken));

    /// <summary>UC20: preview (confirm false) or import of the valid rows.</summary>
    [HttpPost("import")]
    public async Task<ActionResult<IReadOnlyList<UserImportRowResult>>> Import(
        UserImportRequest request,
        [FromServices] UserImportService import,
        CancellationToken cancellationToken)
    {
        var rows = await import.ImportAsync(request, cancellationToken);
        return rows is null ? ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["rows"] = ["InvalidValue"] })) : Ok(rows);
    }

    private ActionResult<UserReadModel> ToResponse(UserActionResult result) =>
        !result.Found ? NotFound()
        : result.Conflict ? Conflict()
        : !result.Succeeded ? Problem(result.Errors)
        : result.User!;

    private ActionResult Problem(IReadOnlyDictionary<string, string> errors) =>
        ValidationProblem(new ValidationProblemDetails(errors.ToDictionary(error => error.Key, error => new[] { error.Value })));
}
