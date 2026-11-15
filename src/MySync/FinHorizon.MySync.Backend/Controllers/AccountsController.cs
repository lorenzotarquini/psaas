using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Portfolio;
using FinHorizon.MySync.Backend.Contracts;
using FinHorizon.MySync.Backend.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FinHorizon.MySync.Backend.Controllers;

/// <summary>UC1, UC2, UC4, UC10: the accounts of the current user.</summary>
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

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccountView>> Update(
        Guid id,
        AccountBody body,
        [FromServices] IRequestHandler<UpdateAccountCommand, AccountView> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var command = new UpdateAccountCommand(id, body.Kind, body.Name, body.Description, body.Tag, body.InitialBalance);
        return (await handler.HandleAsync(command, cancellationToken)).ToActionResult(this);
    }
}
