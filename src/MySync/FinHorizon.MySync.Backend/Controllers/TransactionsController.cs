using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Backend.Contracts;
using FinHorizon.MySync.Backend.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FinHorizon.MySync.Backend.Controllers;

/// <summary>UC5 and UC8: the movements of the current user.</summary>
[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TransactionPage>> List(
        [FromQuery] ListTransactionsQuery query,
        [FromServices] IRequestHandler<ListTransactionsQuery, TransactionPage> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(query, cancellationToken)).ToActionResult(this);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionView>> Get(
        Guid id,
        [FromServices] IRequestHandler<GetTransactionQuery, TransactionView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetTransactionQuery(id), cancellationToken)).ToActionResult(this);

    [HttpPost]
    public async Task<ActionResult<TransactionView>> Record(
        RecordTransactionCommand command,
        [FromServices] IRequestHandler<RecordTransactionCommand, TransactionView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(command, cancellationToken)).ToActionResult(this);

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionView>> Amend(
        Guid id,
        TransactionBody body,
        [FromServices] IRequestHandler<AmendTransactionCommand, TransactionView> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var command = new AmendTransactionCommand(id, body.Kind, body.Date, body.Amount, body.AccountId, body.CategoryId, body.DebtId, body.Note, body.CounterAccountId);
        return (await handler.HandleAsync(command, cancellationToken)).ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IRequestHandler<DeleteTransactionCommand, Unit> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new DeleteTransactionCommand(id), cancellationToken)).ToNoContent(this);
}
