using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Subscriptions;
using FinHorizon.MySync.Backend.Contracts;
using FinHorizon.MySync.Backend.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FinHorizon.MySync.Backend.Controllers;

/// <summary>UC16: the subscriptions of the current user.</summary>
[ApiController]
[Route("api/subscriptions")]
public sealed class SubscriptionsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SubscriptionList>> List(
        [FromServices] IRequestHandler<ListSubscriptionsQuery, SubscriptionList> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListSubscriptionsQuery(), cancellationToken)).ToActionResult(this);

    [HttpPost]
    public async Task<ActionResult<SubscriptionView>> Create(
        CreateSubscriptionCommand command,
        [FromServices] IRequestHandler<CreateSubscriptionCommand, SubscriptionView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(command, cancellationToken)).ToActionResult(this);

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SubscriptionView>> Update(
        Guid id,
        SubscriptionBody body,
        [FromServices] IRequestHandler<UpdateSubscriptionCommand, SubscriptionView> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var command = new UpdateSubscriptionCommand(id, body.Name, body.Description, body.AccountId, body.CategoryId, body.Amount, body.FirstChargeOn);
        return (await handler.HandleAsync(command, cancellationToken)).ToActionResult(this);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<SubscriptionView>> Cancel(
        Guid id,
        CancelSubscriptionBody body,
        [FromServices] IRequestHandler<CancelSubscriptionCommand, SubscriptionView> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        return (await handler.HandleAsync(new CancelSubscriptionCommand(id, body.EndsOn), cancellationToken)).ToActionResult(this);
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<ActionResult<SubscriptionView>> Resume(
        Guid id,
        [FromServices] IRequestHandler<ResumeSubscriptionCommand, SubscriptionView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ResumeSubscriptionCommand(id), cancellationToken)).ToActionResult(this);

    [HttpPost("{id:guid}/skip")]
    public async Task<ActionResult<SubscriptionView>> Skip(
        Guid id,
        SkipChargeBody body,
        [FromServices] IRequestHandler<SkipSubscriptionChargeCommand, SubscriptionView> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        return (await handler.HandleAsync(new SkipSubscriptionChargeCommand(id, body.DueOn), cancellationToken)).ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IRequestHandler<DeleteSubscriptionCommand, Unit> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new DeleteSubscriptionCommand(id), cancellationToken)).ToNoContent(this);
}
