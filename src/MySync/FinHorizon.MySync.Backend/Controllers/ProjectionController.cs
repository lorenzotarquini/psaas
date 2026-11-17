using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Projection;
using FinHorizon.MySync.Backend.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FinHorizon.MySync.Backend.Controllers;

/// <summary>UC14: what the frontend needs to project the net worth, and the one projection the user saves.</summary>
[ApiController]
[Route("api/projection")]
public sealed class ProjectionController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProjectionView>> Get(
        [FromServices] IRequestHandler<GetProjectionQuery, ProjectionView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetProjectionQuery(), cancellationToken)).ToActionResult(this);

    [HttpPut]
    public async Task<ActionResult<SavedProjectionView>> Save(
        SaveProjectionCommand command,
        [FromServices] IRequestHandler<SaveProjectionCommand, SavedProjectionView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(command, cancellationToken)).ToActionResult(this);

    /// <summary>UC14-10b "Riparti dai miei dati".</summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromServices] IRequestHandler<DeleteProjectionCommand, Unit> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new DeleteProjectionCommand(), cancellationToken)).ToNoContent(this);
}
