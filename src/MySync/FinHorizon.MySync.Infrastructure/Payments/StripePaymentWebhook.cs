using System.Text.Json;
using FinHorizon.MySync.Application.Abstractions.Payments;
using Microsoft.Extensions.Options;
using NLog;
using Stripe;

namespace FinHorizon.MySync.Infrastructure.Payments;

/// <summary>
/// UC17-S8: signature checked with the official library; the event itself is read as plain JSON for its customer only,
/// so a different API version of the account cannot break it (the state is read again from Stripe, R5).
/// </summary>
internal sealed class StripePaymentWebhook(IOptions<StripeOptions> options) : IPaymentWebhook
{
    private const long ToleranceSeconds = 300;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.WebhookSecret);

    public PaymentNotification? Verify(string payload, string? signature)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(signature) || string.IsNullOrEmpty(payload))
        {
            return null;
        }

        try
        {
            EventUtility.ValidateSignature(payload, signature, options.Value.WebhookSecret, ToleranceSeconds);
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            var data = root.GetProperty("data").GetProperty("object");
            return new PaymentNotification(root.GetProperty("id").GetString()!, root.GetProperty("type").GetString()!, CustomerOf(data));
        }
        catch (Exception exception) when (exception is StripeException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            // Never the payload: it holds customer data (RNF7).
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
