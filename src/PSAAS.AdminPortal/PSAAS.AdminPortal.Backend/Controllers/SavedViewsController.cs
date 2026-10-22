using Microsoft.AspNetCore.Mvc;
using PSAAS.Application.Contracts.Views;
using PSAAS.Application.Services.Views;

namespace PSAAS.AdminPortal.Backend.Controllers;

// UC22: views of the current administrator only.
[ApiController]
[Route("api/saved-views")]
public sealed class SavedViewsController(SavedViewService views) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<SavedView>> Get([FromQuery] string area, CancellationToken cancellationToken) =>
        views.GetAsync(area, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<SavedView>> Save(SavedViewRequest request, CancellationToken cancellationToken)
    {
        var result = await views.SaveAsync(request, cancellationToken);
        if (result.View is null)
        {
            return ValidationProblem(new ValidationProblemDetails(result.Errors.ToDictionary(e => e.Key, e => new[] { e.Value })));
        }

        return StatusCode(StatusCodes.Status201Created, result.View);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await views.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
