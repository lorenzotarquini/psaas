namespace FinHorizon.AdminPortal.Application.Contracts.Payments;

/// <summary>An event of the payment provider (Stripe), already verified: only what the portal keeps of it (UC33).</summary>
public sealed record PaymentEvent(string Id, string Type, DateTimeOffset CreatedAt);

/// <summary>A received event as stored and listed (UC33 step 4).</summary>
public sealed record ReceivedPaymentEvent(string Id, string Type, DateTimeOffset CreatedAt, DateTimeOffset ReceivedAt);

public enum PaymentEventOutcome
{
    Recorded,
    /// <summary>Same id already received (UC33-R2): acknowledged, nothing else happens.</summary>
    Duplicate,
}
