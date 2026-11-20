using FinHorizon.MySync.Application.Abstractions.Payments;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Memberships;
using FinHorizon.MySync.Backend.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinHorizon.MySync.Backend.Controllers;

/// <summary>UC17: the subscription of the current user to MySync, paid on Stripe.</summary>
[ApiController]
[Route("api/membership")]
public sealed class MembershipController : ControllerBase
{
    /// <summary>The realm role of the group end_users: only they are Stripe customers (UC17-R2).</summary>
    public const string EndUserRole = "enduser";

    [HttpGet]
    public async Task<ActionResult<MembershipView>> Get(
        [FromServices] IRequestHandler<GetMembershipQuery, MembershipView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetMembershipQuery(), cancellationToken)).ToActionResult(this);

    /// <summary>UC17 step 2: the plans of the MySync module on sale (UC16 of the Admin Portal).</summary>
    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<MembershipPlanView>>> Plans(
        [FromServices] IRequestHandler<ListMembershipPlansQuery, IReadOnlyList<MembershipPlanView>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListMembershipPlansQuery(), cancellationToken)).ToActionResult(this);

    [HttpPost("checkout")]
    public async Task<ActionResult<RedirectView>> Checkout(
        CheckoutBody body,
        [FromServices] IRequestHandler<StartCheckoutCommand, RedirectView> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (!User.IsInRole(EndUserRole))
        {
            return Forbid();
        }

        var name = User.FindFirst("name")?.Value ?? User.FindFirst("preferred_username")?.Value;
        return (await handler.HandleAsync(new StartCheckoutCommand(body.PlanCode, User.FindFirst("email")?.Value, name), cancellationToken)).ToActionResult(this);
    }

    [HttpPost("portal")]
    public async Task<ActionResult<RedirectView>> Portal(
        [FromServices] IRequestHandler<OpenBillingPortalCommand, RedirectView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new OpenBillingPortalCommand(), cancellationToken)).ToActionResult(this);

    [HttpPost("refresh")]
    public async Task<ActionResult<MembershipView>> Refresh(
        [FromServices] IRequestHandler<RefreshMembershipCommand, MembershipView> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new RefreshMembershipCommand(), cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Events of Stripe: no Keycloak token, the signature of the exact body is the authentication (UC17-S8).
    /// Any verified event answers 200, also for customers that are not MySync's (S9), so Stripe does not resend it.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        [FromServices] IPaymentWebhook webhook,
        [FromServices] MembershipSync sync,
        CancellationToken cancellationToken)
    {
        if (!webhook.IsConfigured)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Stripe webhook not configured");
        }

        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        if (webhook.Verify(payload, Request.Headers["Stripe-Signature"]) is not { } notification)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid Stripe signature");
        }

        await sync.OnNotificationAsync(notification, cancellationToken).ConfigureAwait(false);
        return Ok();
    }
}

/// <summary>UC17 step 2: the plan chosen by the user.</summary>
public sealed record CheckoutBody(string? PlanCode);
