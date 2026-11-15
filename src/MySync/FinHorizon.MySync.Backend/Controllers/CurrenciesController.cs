using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Currencies;
using FinHorizon.MySync.Application.Features.Preferences;
using FinHorizon.MySync.Backend.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FinHorizon.MySync.Backend.Controllers;

/// <summary>UC13: the catalog of currencies.</summary>
[ApiController]
[Route("api/currencies")]
public sealed class CurrenciesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CurrencyInfo>>> List(
        [FromServices] IRequestHandler<ListCurrenciesQuery, IReadOnlyList<CurrencyInfo>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListCurrenciesQuery(), cancellationToken)).ToActionResult(this);
}

/// <summary>UC1, UC11, UC13: the display currency of the current user.</summary>
[ApiController]
[Route("api/preferences")]
public sealed class PreferencesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PreferencesView>> Get(
        [FromServices] IRequestHandler<GetPreferencesQuery, PreferencesView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetPreferencesQuery(), cancellationToken)).ToActionResult(this);

    [HttpPut]
    public async Task<ActionResult<PreferencesView>> Update(
        UpdatePreferencesCommand command,
        [FromServices] IRequestHandler<UpdatePreferencesCommand, PreferencesView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(command, cancellationToken)).ToActionResult(this);
}