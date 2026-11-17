using NLog;
using Stripe;
using FinHorizon.AdminPortal.Application.Abstractions.Payments;
using FinHorizon.AdminPortal.Application.Contracts.Payments;

namespace FinHorizon.AdminPortal.Infrastructure.Payments;

/// <summary>
/// UC33 step 2 with the official library: HMAC of the payload with the endpoint secret, timestamp within 5 minutes.
/// The secret is Stripe:WebhookSecret (whsec_…), from the Stripe CLI locally, from the Dashboard endpoint in production.
/// </summary>
public sealed class StripeWebhookVerifier(string? webhookSecret) : IPaymentWebhookVerifier
{
    private const long ToleranceSeconds = 300;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public bool IsConfigured => !string.IsNullOrWhiteSpace(webhookSecret);

    public PaymentEvent? Verify(string payload, string? signature)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(signature))
        {
            return null;
        }

        try
        {
            // Events keep the API version of the account or endpoint; until the portal reads their objects
            // a different version is not a reason to refuse them.
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret, ToleranceSeconds, throwOnApiVersionMismatch: false);
            return new PaymentEvent(stripeEvent.Id, stripeEvent.Type, new DateTimeOffset(DateTime.SpecifyKind(stripeEvent.Created, DateTimeKind.Utc)));
        }
        catch (Exception exception) when (exception is StripeException or Newtonsoft.Json.JsonException or ArgumentException)
        {
            // The payload is not logged: it may hold customer data (UC33-R4).
            Logger.Warn("Stripe webhook refused: {Reason}", exception.Message);
            return null;
        }
    }
}
