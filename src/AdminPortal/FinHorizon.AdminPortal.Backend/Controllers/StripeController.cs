using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using FinHorizon.AdminPortal.Application.Abstractions.Payments;
using FinHorizon.AdminPortal.Application.Contracts.Payments;
using FinHorizon.AdminPortal.Application.Services.Payments;

namespace FinHorizon.AdminPortal.Backend.Controllers;

// UC33: events of Stripe (docs/admin-portal/STRIPE.md).
[ApiController]
[Route("api/stripe")]
public sealed class StripeController(PaymentEventService events, IPaymentWebhookVerifier verifier) : ControllerBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// UC33-R1: Stripe has no Keycloak token, the signature of the body is the authentication. The body is read
    /// as it arrives: the signature covers the exact bytes, so it must not go through model binding.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        if (!verifier.IsConfigured)
        {
            // UC33 flow 2b: Stripe retries, so nothing is lost while the secret is being set.
            Logger.Warn("Stripe webhook called but Stripe:WebhookSecret is not configured");
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Stripe webhook not configured");
        }

        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        if (verifier.Verify(payload, Request.Headers["Stripe-Signature"]) is not { } paymentEvent)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid Stripe signature");
        }

        await events.ReceiveAsync(paymentEvent, cancellationToken);
        return Ok();
    }

    /// <summary>UC33 step 4 and R5.</summary>
    [HttpGet("events")]
    public async Task<ActionResult<IReadOnlyList<ReceivedPaymentEvent>>> GetEvents(CancellationToken cancellationToken, [FromQuery] int take = 50)
    {
        if (take is < 1 or > 200)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["take"] = ["InvalidValue"] }));
        }

        return Ok(await events.GetLatestAsync(take, cancellationToken));
    }
}
