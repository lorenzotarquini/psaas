using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Debts;
using FinHorizon.MySync.Application.Features.Portfolio;
using FinHorizon.MySync.Backend.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FinHorizon.MySync.Backend.Controllers;

/// <summary>UC1, UC3, UC4: the debts of the current user.</summary>
[ApiController]
[Route("api/debts")]
public sealed class DebtsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DebtList>> List(
        [FromServices] IRequestHandler<ListDebtsQuery, DebtList> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListDebtsQuery(), cancellationToken)).ToActionResult(this);

    [HttpPost]
    public async Task<ActionResult<DebtView>> Create(
        CreateDebtCommand command,
        [FromServices] IRequestHandler<CreateDebtCommand, DebtView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(command, cancellationToken)).ToActionResult(this);
}
