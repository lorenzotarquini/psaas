using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Portfolio;
using FinHorizon.MySync.Backend.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FinHorizon.MySync.Backend.Controllers;

/// <summary>UC1, UC2, UC4: the accounts of the current user.</summary>
[ApiController]
[Route("api/accounts")]
public sealed class AccountsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AccountList>> List(
        [FromServices] IRequestHandler<ListAccountsQuery, AccountList> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListAccountsQuery(), cancellationToken)).ToActionResult(this);

    [HttpPost]
    public async Task<ActionResult<AccountView>> Create(
        CreateAccountCommand command,
        [FromServices] IRequestHandler<CreateAccountCommand, AccountView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(command, cancellationToken)).ToActionResult(this);
}
