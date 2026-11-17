namespace FinHorizon.MySync.Infrastructure.Payments;

/// <summary>Stripe keys (UC17). Locally from the AppHost parameters stripe-secret-key and stripe-webhook-secret.</summary>
public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    /// <summary>sk_test_… / sk_live_…; empty = payments unavailable (503), the rest of MySync works.</summary>
    public string? SecretKey { get; set; }

    /// <summary>whsec_… of the endpoint (or of the Stripe CLI locally); empty = webhook answers 503.</summary>
    public string? WebhookSecret { get; set; }
}
