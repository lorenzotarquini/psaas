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

    /// <summary>
    /// UC17-R1: the plans on sale for MySync, defined in the Admin Portal (UC16) and read from Stripe, cheapest first.
    /// </summary>
    Task<IReadOnlyList<MembershipPlan>> GetPlansAsync(CancellationToken cancellationToken = default);

    /// <summary>Stripe's payment page (Checkout) for the plan, with its VAT and, when asked, its trial; back to <paramref name="returnUrl"/>.</summary>
    Task<Uri> CreateCheckoutAsync(string customerId, OwnerId ownerId, MembershipPlan plan, bool withTrial, Uri returnUrl, CancellationToken cancellationToken = default);

    /// <summary>Stripe's customer portal: payment method, invoices, cancellation (UC17 flow 1a).</summary>
    Task<Uri> CreatePortalAsync(string customerId, Uri returnUrl, CancellationToken cancellationToken = default);

    /// <summary>The state of the customer's MySync subscription now (R5).</summary>
    Task<MembershipSnapshot> GetSnapshotAsync(string customerId, CancellationToken cancellationToken = default);
}

/// <summary>A plan of the MySync module on Stripe (UC16 of the Admin Portal): what the user chooses from.</summary>
public sealed record MembershipPlan(
    string Code,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string Interval,
    bool PriceIncludesVat,
    decimal? VatRate,
    int TrialDays,
    string PriceId,
    string? TaxRateId);

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
