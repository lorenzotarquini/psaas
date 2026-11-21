using System.Text.Json;
using NLog;
using Stripe;

namespace FinHorizon.Modules.Billing;

/// <summary>
/// Signature checked with the official library (5 minutes of tolerance); the event itself is read as plain JSON for
/// its customer only, so a different API version of the account cannot break it.
/// </summary>
public sealed class StripeWebhookVerifier(ModuleBillingOptions options)
{
    private const long ToleranceSeconds = 300;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.WebhookSecret);

    public BillingNotification? Verify(string payload, string? signature)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(signature) || string.IsNullOrEmpty(payload))
        {
            return null;
        }

        try
        {
            EventUtility.ValidateSignature(payload, signature, options.WebhookSecret, ToleranceSeconds);
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            var data = root.GetProperty("data").GetProperty("object");
            return new BillingNotification(root.GetProperty("id").GetString()!, root.GetProperty("type").GetString()!, CustomerOf(data));
        }
        catch (Exception exception) when (exception is StripeException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            // Never the payload: it holds customer data.
            Logger.Warn("Stripe webhook refused: {Reason}", exception.Message);
            return null;
        }
    }

    /// <summary>The customer of a subscription, invoice, payment intent or checkout session; a customer is its own.</summary>
    private static string? CustomerOf(JsonElement data)
    {
        if (data.TryGetProperty("object", out var kind) && kind.GetString() == "customer")
        {
            return data.GetProperty("id").GetString();
        }

        return data.TryGetProperty("customer", out var customer) && customer.ValueKind == JsonValueKind.String ? customer.GetString() : null;
    }
}
