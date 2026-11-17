using FinHorizon.AdminPortal.Application.Contracts.Payments;

namespace FinHorizon.AdminPortal.Application.Abstractions.Payments;

/// <summary>Checks that a webhook call really comes from the payment provider (UC33 step 2).</summary>
public interface IPaymentWebhookVerifier
{
    /// <summary>False when the webhook secret is missing (UC33 flow 2b).</summary>
    bool IsConfigured { get; }

    /// <summary>The event when the signature is valid and recent; null otherwise (UC33 flow 2a).</summary>
    PaymentEvent? Verify(string payload, string? signature);
}
