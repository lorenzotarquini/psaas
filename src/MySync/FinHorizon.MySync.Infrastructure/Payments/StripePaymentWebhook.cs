using FinHorizon.Modules.Billing;
using FinHorizon.MySync.Application.Abstractions.Payments;
using Microsoft.Extensions.Options;

namespace FinHorizon.MySync.Infrastructure.Payments;

/// <summary>UC17-S8 through the shared verifier (FinHorizon.Modules.Billing).</summary>
internal sealed class StripePaymentWebhook(IOptions<StripeOptions> options) : IPaymentWebhook
{
    private readonly StripeWebhookVerifier _verifier = new(new ModuleBillingOptions { WebhookSecret = options.Value.WebhookSecret });

    public bool IsConfigured => _verifier.IsConfigured;

    public PaymentNotification? Verify(string payload, string? signature) =>
        _verifier.Verify(payload, signature) is { } notification ? new PaymentNotification(notification.EventId, notification.Type, notification.CustomerId) : null;
}
