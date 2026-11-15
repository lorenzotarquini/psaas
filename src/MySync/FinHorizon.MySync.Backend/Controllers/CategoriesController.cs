using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Categories;
using FinHorizon.MySync.Backend.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FinHorizon.MySync.Backend.Controllers;

/// <summary>UC6: categories searched and created while entering a movement.</summary>
[ApiController]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryView>>> Search(
        [FromQuery] string? kind,
        [FromQuery] string? term,
        [FromServices] IRequestHandler<SearchCategoriesQuery, IReadOnlyList<CategoryView>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new SearchCategoriesQuery(kind, term), cancellationToken)).ToActionResult(this);

    /// <summary>201 with a new category, 200 with the one that already existed (UC6-R4).</summary>
    [HttpPost]
    public async Task<ActionResult<CategoryView>> Create(
        CreateCategoryCommand command,
        [FromServices] IRequestHandler<CreateCategoryCommand, CategoryView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(command, cancellationToken)).ToActionResult(this);
}
