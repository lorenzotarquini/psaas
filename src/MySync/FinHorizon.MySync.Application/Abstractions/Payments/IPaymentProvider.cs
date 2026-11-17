using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Memberships;

namespace FinHorizon.MySync.Application.Abstractions.Payments;

/// <summary>
/// UC17: Stripe as seen by the use cases. Prices, emails, retries and invoices are Stripe's (R7): MySync only creates
/// the customer, sends the user to Stripe's pages and reads back the state.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>A new customer for the user (R2); the same user never gets two, even when asked twice at once.</summary>
    Task<string> CreateCustomerAsync(OwnerId ownerId, string? email, string? name, CancellationToken cancellationToken = default);

    /// <summary>Stripe's payment page (Checkout) for the MySync price, back to <paramref name="returnUrl"/>.</summary>
    Task<Uri> CreateCheckoutAsync(string customerId, OwnerId ownerId, Uri returnUrl, CancellationToken cancellationToken = default);

    /// <summary>Stripe's customer portal: payment method, invoices, cancellation (UC17 flow 1a).</summary>
    Task<Uri> CreatePortalAsync(string customerId, Uri returnUrl, CancellationToken cancellationToken = default);

    /// <summary>The state of the customer's MySync subscription now (R5).</summary>
    Task<MembershipSnapshot> GetSnapshotAsync(string customerId, CancellationToken cancellationToken = default);
}

/// <summary>A verified event of Stripe: only which customer it concerns, the rest is read again from Stripe (R5).</summary>
public sealed record PaymentNotification(string EventId, string Type, string? CustomerId);

/// <summary>Checks that a webhook call comes from Stripe (UC17-S8).</summary>
public interface IPaymentWebhook
{
    bool IsConfigured { get; }

    /// <summary>The event when the signature is valid and recent; null otherwise.</summary>
    PaymentNotification? Verify(string payload, string? signature);
}

/// <summary>Stripe cannot be reached or is not configured: the request cannot be answered now (503).</summary>
public sealed class PaymentProviderUnavailableException : Exception
{
    public PaymentProviderUnavailableException()
    {
    }

    public PaymentProviderUnavailableException(string message)
        : base(message)
    {
    }

    public PaymentProviderUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
